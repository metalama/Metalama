// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Introspection;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// Maintains the state of <see cref="HighLevelPipelineStage"/>, composed of several <see cref="PipelineStep"/>.
/// The current object is essentially a mutable list of <see cref="PipelineStep"/> instances. It exposes methods
/// like <see cref="AddAspectInstances"/> or <see cref="AddAspectSources"/> that
/// allow to add inputs to different steps of the pipeline. This object must create the steps in the appropriate order.
/// </summary>
internal sealed class PipelineStepsState
{
    private readonly SkipListDictionary<PipelineStepId, PipelineStep> _steps;
    private readonly PipelineStepIdComparer _comparer;
    private readonly ConcurrentLinkedList<ITransformation> _transformations = new();
    private readonly ConcurrentLinkedList<IAspectInstance> _inheritableAspectInstances = new();
    private readonly ConcurrentLinkedList<AspectInstanceResult> _aspectInstanceResults = new();
    private readonly ConcurrentLinkedList<IExtensionPipelineContributor> _extensionContributors = new();
    private readonly OverflowAspectSource _overflowAspectSource = new();
    private readonly IntrospectionPipelineListener? _introspectionListener;
    private readonly bool _shouldDetectUnorderedAspects;
    private readonly IConcurrentTaskRunner _concurrentTaskRunner;

    private PipelineStep? _currentStep;

    private CompilationModel LastCompilation { get; set; }

    public CompilationModel FirstCompilation { get; }
    
    private UserDiagnosticSink Diagnostics { get; }

    public AspectPipelineConfiguration PipelineConfiguration { get; }

    public PipelineStepsState(
        IReadOnlyList<OrderedAspectLayer> aspectLayers,
        CompilationModel inputLastCompilation,
        PipelineContributorSources sources,
        AspectPipelineConfiguration pipelineConfiguration,
        CancellationToken cancellationToken )
    {
        this._concurrentTaskRunner = pipelineConfiguration.ServiceProvider.GetRequiredService<IConcurrentTaskRunner>();
        this._introspectionListener = pipelineConfiguration.ServiceProvider.GetService<IntrospectionPipelineListener>();
        this._shouldDetectUnorderedAspects = pipelineConfiguration.ServiceProvider.GetRequiredService<IProjectOptions>().RequireOrderedAspects;

        this.Diagnostics = new UserDiagnosticSink( pipelineConfiguration.ServiceProvider );
        this.LastCompilation = this.FirstCompilation = inputLastCompilation;
        this.PipelineConfiguration = pipelineConfiguration;

        // Create an empty collection of steps.
        this._comparer = new PipelineStepIdComparer( aspectLayers );
        this._steps = new SkipListDictionary<PipelineStepId, PipelineStep>( this._comparer );

        // Add the initial steps.
        foreach ( var aspectLayer in aspectLayers )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if ( aspectLayer.AspectLayerId.IsDefault )
            {
                var step = new EvaluateAspectSourcesPipelineStep( this, aspectLayer );

                _ = this._steps.Add( step.Id, step );
            }
        }

        // Add the initial sources.
        // TODO: process failure of the next line.
        this.AddAspectSources( sources.Contributors.OfType<IAspectSource>(), false, cancellationToken );
        this.AddExtendedContributors( sources.Contributors.OfType<IExtensionPipelineContributor>() );
    }

    public async Task<PipelineStepsResult> ExecuteAsync( CancellationToken cancellationToken )
    {
        using var enumerator = this._steps.GetEnumerator();
        PipelineStep? previousStep = null;

        var stepIndex = 0;

        while ( enumerator.MoveNext() )
        {
            cancellationToken.ThrowIfCancellationRequested();

            this._currentStep = enumerator.Current.Value;

            this.DetectUnorderedSteps( ref previousStep, this._currentStep );

            var compilation = this.LastCompilation;

            this.LastCompilation = await this._currentStep!.ExecuteAsync( compilation, this.Diagnostics, stepIndex, cancellationToken );

            stepIndex++;
        }

        return new PipelineStepsResult(
            this.FirstCompilation,
            this.LastCompilation,
            this._transformations,
            this._inheritableAspectInstances,
            this.Diagnostics.ToImmutable(),
            this._overflowAspectSource,
            this._extensionContributors,
            this._aspectInstanceResults );
    }

    private void DetectUnorderedSteps( ref PipelineStep? previousStep, PipelineStep currentStep )
    {
        if ( previousStep != null && this._shouldDetectUnorderedAspects )
        {
            if ( previousStep.AspectLayer != currentStep.AspectLayer && previousStep.AspectLayer.ExplicitOrder >= currentStep.AspectLayer.ExplicitOrder )
            {
                this.Diagnostics.Report(
                    GeneralDiagnosticDescriptors.UnorderedLayers.CreateRoslynDiagnostic(
                        null,
                        (previousStep.AspectLayer.AspectLayerId.ToString(), currentStep.AspectLayer.AspectLayerId.ToString()) ) );
            }

            if ( this._comparer.Compare( currentStep.Id, previousStep.Id ) < 0 )
            {
                throw new AssertionFailedException( "Steps with lower depth must be processed before steps with higher depth." );
            }
        }

        previousStep = currentStep;
    }

    public void AddAspectSources( IEnumerable<IAspectSource> aspectSources, bool addOtherStagesToOverflow, CancellationToken cancellationToken )
    {
        foreach ( var aspectSource in aspectSources )
        {
            foreach ( var aspectType in aspectSource.AspectClasses )
            {
                cancellationToken.ThrowIfCancellationRequested();

                if ( this._currentStep?.AspectLayer.AspectClass == aspectType )
                {
                    // When an aspect class is adding an instance of the same aspect class, we need to evaluate
                    // the aspect source immediately. This is done by the caller, and not here, so we skip it.
                }
                else
                {
                    var aspectLayerId = new AspectLayerId( aspectType );

                    if ( !this._comparer.Contains( aspectLayerId ) )
                    {
                        // This is an aspect of a different stage.
                        if ( this._currentStep != null )
                        {
                            var currentLayerIndex = this.PipelineConfiguration.AspectLayers.IndexOf( this._currentStep.AspectLayer );

                            Invariant.Assert( currentLayerIndex != -1 );

                            // Find out if there is no future layer for this aspect type.
                            if ( !this.PipelineConfiguration.AspectLayers.Skip( currentLayerIndex + 1 ).Any( layer => layer.Equals( aspectLayerId ) ) )
                            {
                                this.Diagnostics.Report(
                                    GeneralDiagnosticDescriptors.CannotAddChildAspectToPreviousPipelineStep.CreateRoslynDiagnostic(
                                        this._currentStep.AspectLayer.AspectClass.GetDiagnosticLocation( this.FirstCompilation.RoslynCompilation ),
                                        (this._currentStep.AspectLayer.AspectClass.ShortName, aspectType.ShortName) ) );

                                continue;
                            }
                        }

                        if ( addOtherStagesToOverflow )
                        {
                            this._overflowAspectSource.Add( aspectSource, aspectType );
                        }
                    }
                    else
                    {
                        // There is a unique depth and TargetKind for the AspectSource step.
                        var stepId = new PipelineStepId( aspectLayerId, -1, -1, -1 );

                        if ( !this.TryGetOrAddStep( stepId, false, out var step ) )
                        {
                            this.Diagnostics.Report(
                                GeneralDiagnosticDescriptors.CannotAddChildAspectToPreviousPipelineStep.CreateRoslynDiagnostic(
                                    this._currentStep!.AspectLayer.AspectClass.GetDiagnosticLocation( this.FirstCompilation.RoslynCompilation ),
                                    (this._currentStep.AspectLayer.AspectClass.ShortName, aspectType.ShortName) ) );

                            continue;
                        }

                        var typedStep = (EvaluateAspectSourcesPipelineStep) step;
                        typedStep.AddAspectSource( aspectSource );
                    }
                }
            }
        }
    }

    public async Task<ImmutableArray<AspectInstance>> ExecuteAspectSourceAsync(
        CompilationModel compilation,
        IAspectClass aspectClass,
        IEnumerable<IAspectSource> aspectSources,
        IUserDiagnosticSink diagnosticSink,
        CancellationToken cancellationToken )
    {
        var collector = new AspectInstanceCollector( aspectClass, compilation, diagnosticSink, cancellationToken );

        await this._concurrentTaskRunner.RunConcurrentlyAsync(
            aspectSources.Where( a => a.AspectClasses.Contains( aspectClass ) ),
            source => source.CollectAspectInstancesAsync( collector ),
            cancellationToken );

        HashSet<IDeclaration>? exclusions = null;

        foreach ( var exclusion in collector.AspectExclusions )
        {
            exclusions ??= new HashSet<IDeclaration>( ReferenceEqualityComparer<IDeclaration>.Instance );

            exclusions.Add( exclusion.GetTarget( compilation ) );
        }

        bool IsExcluded( IDeclaration declaration )
        {
            if ( exclusions == null || exclusions.Count == 0 )
            {
                return false;
            }

            if ( exclusions.Contains( declaration ) )
            {
                return true;
            }

            if ( declaration.ContainingDeclaration != null )
            {
                return IsExcluded( declaration.ContainingDeclaration );
            }

            return false;
        }

        var aspectInstances = collector.AspectInstances
            .SelectAsReadOnlyCollection(
                x =>
                {
                    var target = (IDeclarationImpl) x.TargetDeclaration.GetTarget( compilation );

                    if ( !x.Predecessors.IsDefaultOrEmpty && x.Predecessors[0].Kind != AspectPredecessorKind.Attribute && IsExcluded( target ) )
                    {
                        // We mark the instance as Skipped so that it is not included in licensing enforcement.
                        x.Skip();

                        return default;
                    }

                    return new ResolvedAspectInstance( x, target, x.ComputeEligibility( target ) );
                } )
            .Where( x => x.TargetDeclaration != null! )
            .ToMutableList();

        // Process aspect requirements.
        // We always add an aspect instance if there is a requirement, even if there is already an instance, because this has the side effect of exposing
        // the predecessors to the IAspectInstance in the last instance of the aggregated aspect.
        var requirements = collector.AspectRequirements
            .SelectAsReadOnlyCollection( x => (TargetDeclaration: x.TargetDeclaration.GetTarget( compilation ), x.Predecessor) )
            .GroupBy( x => x.TargetDeclaration );

        foreach ( var requirement in requirements )
        {
            var requirementTarget = requirement.Key;
            var predecessors = requirement.Select( x => new AspectPredecessor( AspectPredecessorKind.RequiredAspect, x.Predecessor ) );

            var stronglyTypedAspectClass = (AspectClass) aspectClass;
            var aspect = stronglyTypedAspectClass.CreateDefaultInstance( compilation.CompilationContext );

            var aspectInstance = new AspectInstance( aspect, requirementTarget, stronglyTypedAspectClass, predecessors.ToImmutableArray() );
            var eligibility = aspectInstance.ComputeEligibility( requirementTarget );

            if ( (eligibility & (EligibleScenarios.Default | EligibleScenarios.Inheritance)) != 0 )
            {
                aspectInstances.Add( new ResolvedAspectInstance( aspectInstance, (IDeclarationImpl) requirementTarget, eligibility ) );
            }
            else
            {
                // The situation should have been detected before.
                throw new AssertionFailedException( $"Cannot add the aspect '{aspectClass.ShortName}' to '{requirementTarget}' because of eligibility. " );
            }
        }

        // We assume that all aspect instances are eligible, but some are eligible only for inheritance.

        // Get the aspects that can be processed, i.e. they are not abstract-only.
        List<ResolvedAspectInstance> concreteAspectInstances = new( aspectInstances.Count );

        foreach ( var aspectInstance in aspectInstances )
        {
            if ( aspectInstance.Eligibility.IncludesAll( EligibleScenarios.Default ) )
            {
                concreteAspectInstances.Add( aspectInstance );
            }
            else
            {
                // We mark the instance as Skipped so that it is not included in licensing enforcement.
                aspectInstance.AspectInstance.Skip();
            }
        }

        // Gets aspects that can be inherited.
        var inheritableAspectInstances = aspectInstances
            .Where( a => a.Eligibility.IncludesAll( EligibleScenarios.Inheritance ) && a.AspectInstance.IsInheritable )
            .ToReadOnlyList();

        // Gets aspects that have been inherited by the source, including abstract instances that can in turn be only inherited. 
        var inheritedAspectInstancesInProject = inheritableAspectInstances
            .SelectMany(
                a => a.TargetDeclaration.GetDerivedDeclarations()
                    .Where( d => !IsExcluded( d ) )
                    .Select( d => (TargetDeclaration: (IDeclarationImpl) d, DerivedAspectInstance: a.AspectInstance.CreateDerivedInstance( d )) )
                    .Select(
                        x => (x.TargetDeclaration, x.DerivedAspectInstance, Eligibility: x.DerivedAspectInstance.ComputeEligibility( x.TargetDeclaration )) )
                    .Where( x => x.Eligibility.IncludesAny( EligibleScenarios.Default | EligibleScenarios.Inheritance ) )
                    .Select(
                        x =>
                            new ResolvedAspectInstance(
                                x.DerivedAspectInstance,
                                x.TargetDeclaration,
                                x.Eligibility ) ) )
            .ToReadOnlyList();

        var concreteInheritedAspectInstancesInProject =
            inheritedAspectInstancesInProject.Where( x => x.Eligibility.IncludesAll( EligibleScenarios.Default ) )
                .ToReadOnlyList();

        // Index these aspects. 
        this.AddAspectInstances( concreteAspectInstances );
        this.AddAspectInstances( concreteInheritedAspectInstancesInProject );

        // Index the inheritable aspects for the aspect manifest. It must also include indirectly inheritable aspect instances.
        this.AddInheritableAspectInstances(
            inheritableAspectInstances.Concat( inheritedAspectInstancesInProject.Where( a => a.AspectInstance.IsInheritable ) )
                .Select( x => x.AspectInstance ) );

        return concreteAspectInstances.Concat( concreteInheritedAspectInstancesInProject ).SelectAsImmutableArray( x => x.AspectInstance );
    }

    private bool TryGetOrAddStep(
        in PipelineStepId stepId,
        bool allowAddToCurrentLayer,
        [NotNullWhen( true )] out PipelineStep? step )
    {
        var aspectLayer = this._comparer.GetOrderedAspectLayer( stepId.AspectLayerId );

        if ( this._currentStep != null )
        {
            var currentLayerOrder = this._currentStep.AspectLayer.Order;

            if ( aspectLayer.Order < currentLayerOrder ||
                 (stepId.AspectLayerId == this._currentStep.AspectLayer.AspectLayerId
                  && (!allowAddToCurrentLayer || this._comparer.Compare( stepId, this._currentStep.Id ) < 0)) )
            {
                // Cannot add a step before the current one.
                step = null;

                return false;
            }
        }

        if ( !this._steps.TryGetValue( stepId, out step ) )
        {
            lock ( this._steps )
            {
                if ( !this._steps.TryGetValue( stepId, out step ) )
                {
                    step = new ExecuteAspectLayerPipelineStep( this, stepId, aspectLayer );
                    _ = this._steps.Add( stepId, step );
                }
            }
        }

        return true;
    }

    private void AddAspectInstances( IEnumerable<ResolvedAspectInstance> aspectInstances )
    {
        foreach ( var aspectInstance in aspectInstances )
        {
            foreach ( var layer in aspectInstance.AspectInstance.AspectClass.Layers )
            {
                var aspectTargetDeclaration = (IDeclaration) aspectInstance.TargetDeclaration;
                var aspectTargetTypeDeclaration = aspectTargetDeclaration.GetClosestNamedType() ?? aspectTargetDeclaration;

                var stepId = new PipelineStepId(
                    new AspectLayerId( aspectInstance.AspectInstance.AspectClass, layer.LayerName ),
                    aspectTargetTypeDeclaration.Depth,
                    aspectTargetDeclaration.Depth,
                    -1 );

                if ( this._currentStep != null && this._comparer.Compare( this._currentStep.Id, stepId ) >= 0 )
                {
                    // The aspect is trying to add an aspect of the same type, which is legal, but to a declaration
                    // of lower depth.
                    var parentAspect = (AspectInstance) aspectInstance.AspectInstance.Predecessors[0].Instance;
                    var parentTarget = parentAspect.TargetDeclaration.GetTarget( aspectTargetDeclaration.Compilation );

                    this.Report(
                        GeneralDiagnosticDescriptors.CannotAddAspectToPreviousPipelineStep.CreateRoslynDiagnostic(
                            parentAspect.GetDiagnosticLocation( aspectTargetDeclaration.Compilation.GetRoslynCompilation() ),
                            (parentAspect.AspectClass.ShortName, parentTarget, parentTarget.DeclarationKind, aspectTargetDeclaration,
                             aspectTargetDeclaration.DeclarationKind) ) );
                }
                else if ( !this.TryGetOrAddStep( stepId, true, out var step ) )
                {
                    // This should not happen here. The source should not have been added.
                    throw new AssertionFailedException( $"A pipeline step was added for '{stepId}'." );
                }
                else
                {
                    ((ExecuteAspectLayerPipelineStep) step).AddAspectInstance( aspectInstance );
                }
            }
        }
    }

    private void AddInheritableAspectInstances( IEnumerable<AspectInstance> inheritedAspectInstances )
    {
        foreach ( var aspectInstance in inheritedAspectInstances )
        {
            this._inheritableAspectInstances.Add( aspectInstance );
        }
    }

    public void AddDiagnostics( ImmutableUserDiagnosticList diagnostics )
    {
        this.Diagnostics.Report( diagnostics.ReportedDiagnostics );
        this.Diagnostics.Suppress( diagnostics.DiagnosticSuppressions );
        this.Diagnostics.AddExtensions( diagnostics.Extensions );
    }

    public void AddTransformations( IEnumerable<ITransformation> transformations )
    {
        foreach ( var transformation in transformations )
        {
            this._transformations.Add( transformation );
        }
    }

    public void AddExtendedContributors( IEnumerable<IExtensionPipelineContributor> contributors )
    {
        foreach ( var source in contributors )
        {
            this._extensionContributors.Add( source );
        }
    }

    public async Task AddOptionsSourcesAsync( IEnumerable<IHierarchicalOptionsSource> optionsSources, CancellationToken cancellationToken )
    {
        foreach ( var source in optionsSources )
        {
            await this.LastCompilation.HierarchicalOptionsManager.AssertNotNull()
                .AddSourceAsync( source, this.LastCompilation, this.Diagnostics, cancellationToken );
        }
    }

    private void Report( Diagnostic diagnostic ) => this.Diagnostics.Report( diagnostic );

    public void AddAspectInstanceResult( AspectInstanceResult aspectInstanceResult )
    {
        this._aspectInstanceResults.Add( aspectInstanceResult );
        this._introspectionListener?.AddAspectResult( aspectInstanceResult );
    }
}