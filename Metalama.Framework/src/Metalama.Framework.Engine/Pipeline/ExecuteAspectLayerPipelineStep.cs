// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// The <see cref="PipelineStep"/> that runs the default layer of each aspect. It runs the aspect initializer method.
/// </summary>
internal sealed class ExecuteAspectLayerPipelineStep : PipelineStep
{
    private readonly List<AspectInstance> _aspectInstances = [];
    private readonly IConcurrentTaskRunner _concurrentTaskRunner;

    public ExecuteAspectLayerPipelineStep( PipelineStepsState parent, PipelineStepId stepId, OrderedAspectLayer aspectLayer )
        : base( parent, stepId, aspectLayer )
    {
        this._concurrentTaskRunner = parent.PipelineConfiguration.ServiceProvider.GetRequiredService<IConcurrentTaskRunner>();
    }

    public void AddAspectInstance( in ResolvedAspectInstance aspectInstance )
    {
        lock ( this._aspectInstances )
        {
            this._aspectInstances.Add( aspectInstance.AspectInstance );
        }
    }

    public override async Task<CompilationModel> ExecuteAsync(
        CompilationModel compilation,
        UserDiagnosticSink diagnostics,
        int stepIndex,
        CancellationToken cancellationToken )
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var aggregateInstances = this._aspectInstances
            .GroupBy( a => a.TargetDeclaration, RefEqualityComparer<IDeclaration>.Default )
            .Select( AggregateAspectInstance.GetInstance )
            .WhereNotNull()
            .Select( a => (TargetDeclaration: a.TargetDeclaration.GetTarget( compilation ), AspectInstance: a) );

        var instancesByType =
            aggregateInstances.GroupBy( a => a.TargetDeclaration.GetClosestNamedType() );

        // This collection will contain the observable transformations that need to be replayed on the compilation.
        var observableTransformations = new ConcurrentQueue<ITransformation>();

        var aspectInstancesOfSameType = new ConcurrentLinkedList<ImmutableArray<AspectInstance>>();

        // The processing order of types is arbitrary. Different types can be processed in parallel.
        await this._concurrentTaskRunner.RunConcurrentlyAsync(
            instancesByType,
            t => this.ProcessTypeAsync(
                t,
                compilation,
                stepIndex,
                observableTransformations.Enqueue,
                aspectInstancesOfSameType,
                diagnostics,
                cancellationToken ),
            cancellationToken );

        var mergedAspectInstancesOfSameType = aspectInstancesOfSameType.Count > 0 ? aspectInstancesOfSameType.SelectMany( t => t ) : null;

        return compilation.WithTransformationsAndAspectInstances(
            observableTransformations,
            mergedAspectInstancesOfSameType,
            $"After ExecuteAspectLayer({this.AspectLayer})" );
    }

    private async Task ProcessTypeAsync(
        IEnumerable<(IDeclaration TargetDeclaration, IAspectInstanceInternal AspectInstance)> aspects,
        CompilationModel compilation,
        int stepIndex,
        Action<ITransformation> addTransformation,
        ConcurrentLinkedList<ImmutableArray<AspectInstance>> aspectInstancesOfSameType,
        UserDiagnosticSink diagnostics,
        CancellationToken cancellationToken )
    {
        var aspectDriver = (AspectDriver) this.AspectLayer.AspectClass.AspectDriver;

        var currentCompilation = compilation;

        var indexWithinType = 0;

        // Order aspects by source order, if possible.
        var orderedAspects = aspects.OrderBy( a => a, AspectInstanceComparer.Instance );

        foreach ( var aspect in orderedAspects )
        {
            // Set the aspect instance order for use by the linker.
            indexWithinType++;

            // Create a snapshot of the compilation (before this aspect).
            var mutableCompilationForThisAspect = currentCompilation.CreateMutableClone( $"Temporary mutable clone for {this.AspectLayer}." );

            // Execute the aspect.
            var aspectResult = await aspectDriver.ExecuteAspectAsync(
                aspect.AspectInstance,
                this.Id.AspectLayerId.LayerName,
                currentCompilation,
                mutableCompilationForThisAspect,
                this.Parent.PipelineConfiguration,
                stepIndex,
                indexWithinType,
                cancellationToken );

            // Make the immutable copy.
            var newCompilation = mutableCompilationForThisAspect.CreateImmutableClone( $"Executing layer {this.AspectLayer}, {indexWithinType}" );

            this.Parent.AddDiagnostics( aspectResult.Diagnostics );

            switch ( aspectResult.Outcome )
            {
                case AdviceOutcome.Error:
                case AdviceOutcome.Ignore:
                    // We don't commit the changes done to the compilation model.
                    break;

                default:
                    // Apply the changes done by the aspects.
                    currentCompilation = newCompilation;

                    this.Parent.AddAspectSources( aspectResult.Contributors.OfType<IAspectSource>(), true, cancellationToken );
                    this.Parent.AddExtendedContributors( aspectResult.Contributors.OfType<IExtensionPipelineContributor>() );
                    await this.Parent.AddOptionsSourcesAsync( aspectResult.Contributors.OfType<IHierarchicalOptionsSource>(), cancellationToken );

                    var transformations = aspectResult.Transformations;
                    var partialCompilation = this.Parent.FirstCompilation.PartialCompilation;

                    // Filter out transformations that are not considered observed by the partial compilation
                    // or by any other aspect.
                    if ( partialCompilation.HasObservabilityFilter )
                    {
                        // ReSharper disable once AccessToModifiedClosure
                        transformations = transformations.Where(
                                t => t.Observability != TransformationObservability.None ||
                                     t is not ISyntaxTreeTransformation syntaxTreeTransformation ||
                                     partialCompilation.IsSyntaxTreeObserved( syntaxTreeTransformation.TransformedSyntaxTree.FilePath ) )
                            .ToImmutableArray();
                    }

                    this.Parent.AddTransformations( transformations );

                    foreach ( var transformation in transformations )
                    {
                        if ( transformation.Observability != TransformationObservability.None )
                        {
                            addTransformation( transformation );
                        }
                    }

                    // Add children of the same aspect type.
                    var newAspectInstances = await this.Parent.ExecuteAspectSourceAsync(
                        compilation,
                        this.AspectLayer.AspectClass,
                        aspectResult.Contributors.OfType<IAspectSource>(),
                        diagnostics,
                        cancellationToken );

                    if ( !newAspectInstances.IsDefaultOrEmpty )
                    {
                        aspectInstancesOfSameType.Add( newAspectInstances );
                    }

                    break;
            }

            this.Parent.AddAspectInstanceResult( aspectResult );
        }
    }

    private sealed class AspectInstanceComparer : Comparer<(IDeclaration TargetDeclaration, IAspectInstanceInternal AspectInstance)>
    {
        public static AspectInstanceComparer Instance { get; } = new();

        private AspectInstanceComparer() { }

        public override int Compare(
            (IDeclaration TargetDeclaration, IAspectInstanceInternal AspectInstance) x,
            (IDeclaration TargetDeclaration, IAspectInstanceInternal AspectInstance) y )
        {
            if ( x.AspectInstance == y.AspectInstance )
            {
                return 0;
            }

            var xPrimarySyntax = x.TargetDeclaration.GetPrimaryDeclarationSyntax();
            var yPrimarySyntax = y.TargetDeclaration.GetPrimaryDeclarationSyntax();

            // Source declarations come before introduced declarations.
            if ( xPrimarySyntax != null && yPrimarySyntax == null )
            {
                return -1;
            }
            else if ( xPrimarySyntax == null && yPrimarySyntax != null )
            {
                return 1;
            }
            else if ( xPrimarySyntax != null && yPrimarySyntax != null )
            {
                if ( xPrimarySyntax.SyntaxTree != yPrimarySyntax.SyntaxTree )
                {
                    var syntaxTreeComparison = StringComparer.Ordinal.Compare( xPrimarySyntax.SyntaxTree.FilePath, yPrimarySyntax.SyntaxTree.FilePath );

                    if ( syntaxTreeComparison != 0 )
                    {
                        return syntaxTreeComparison;
                    }
                }

                var positionComparison = xPrimarySyntax.SpanStart.CompareTo( yPrimarySyntax.SpanStart );

                if ( positionComparison != 0 )
                {
                    return positionComparison;
                }

                if ( x.TargetDeclaration.Equals( y.TargetDeclaration ) )
                {
                    return 0;
                }

                // Implicitly declared record methods have the same span, compare them by signature.
                if ( x.TargetDeclaration is IMethod xMethod && y.TargetDeclaration is IMethod yMethod )
                {
                    Invariant.Assert(
                        ReferenceEquals( xMethod.DeclaringType, yMethod.DeclaringType )
                        && xMethod.DeclaringType.TypeKind is TypeKind.RecordClass or TypeKind.RecordStruct
                        && xMethod.IsImplicitlyDeclared && yMethod.IsImplicitlyDeclared );

                    var signatureComparison = StructuralSymbolComparer.Signature.Compare( xMethod.GetSymbol(), yMethod.GetSymbol() );

                    if ( signatureComparison != 0 )
                    {
                        return signatureComparison;
                    }
                }

                throw new AssertionFailedException( $"The pair {x} and {y} is not ordered." );
            }
            else
            {
                // If both declarations are introduced, we compare the string rendering.

                return StringComparer.Ordinal.Compare( x.TargetDeclaration.ToString(), y.TargetDeclaration.ToString() );
            }
        }
    }
}