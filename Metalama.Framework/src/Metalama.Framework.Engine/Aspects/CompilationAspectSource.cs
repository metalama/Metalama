// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// An implementation  of <see cref="IAspectSource"/> that creates aspect instances from custom attributes
/// found in a compilation.
/// </summary>
internal sealed class CompilationAspectSource : IAspectSource
{
    private readonly UserCodeAttributeDeserializer.Provider _attributeDeserializerProvider;
    private readonly IConcurrentTaskRunner _concurrentTaskRunner;
    private readonly HashSet<AspectClass> _aspectClasses;
    private ImmutableDictionaryOfArray<IType, IRef<IDeclaration>>? _exclusions;

    public CompilationAspectSource( in ProjectServiceProvider serviceProvider, AspectClassCollection aspectClasses )
    {
        this._attributeDeserializerProvider = serviceProvider.GetRequiredService<UserCodeAttributeDeserializer.Provider>();
        this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();

        // This source only supports real aspects, not fabric aspects.
        this._aspectClasses = aspectClasses.OfType<AspectClass>().ToHashSet();
    }

    public IEnumerable<IAspectClass> AspectClasses => this._aspectClasses;

    private ImmutableDictionaryOfArray<IType, IRef<IDeclaration>> DiscoverExclusions( CompilationModel compilation )
    {
        if ( this._exclusions == null )
        {
            var excludeAspectType = compilation.Factory.GetNamedTypeByReflectionType( typeof(ExcludeAspectAttribute) );

            var attributes = compilation.GetAllAttributesOfType( excludeAspectType )
                .SelectMany(
                    a => a.ConstructorArguments[0]
                        .Values.Select( arg => (TargetDeclaration: a.ContainingDeclaration, AspectType: (IType) arg.Value!) ) )
                .SelectMany(
                    x => x.TargetDeclaration switch
                    {
                        IHasAccessors hasAccessors => hasAccessors.Accessors.Select( a => x with { TargetDeclaration = a } ).Concat( x ),
                        _ => [x]
                    } );

            this._exclusions =
                attributes
                    .ToMultiValueDictionary( x => x.AspectType, x => x.TargetDeclaration.ToRef() );
        }

        return this._exclusions;
    }

    public bool ContainsAspectClass( IAspectClass aspectClass ) => aspectClass is AspectClass typed && this._aspectClasses.Contains( typed );

    public Task CollectAspectInstancesAsync( AspectInstanceCollector collector )
    {
        var attributeDeserializer = this._attributeDeserializerProvider.Get( collector.Compilation.CompilationContext );

        var aspectClass = (AspectClass) collector.AspectClass;

        if ( !collector.Compilation.Factory.TryGetTypeByReflectionName( aspectClass.FullName, out var aspectType ) )
        {
            // This happens at design time when the IDE sends an incomplete compilation. We cannot apply the aspects in this case,
            // but we prefer not to throw an exception since the case is expected.
            return Task.CompletedTask;
        }

        // Process exclusions.
        var exclusions = this.DiscoverExclusions( collector.Compilation )[aspectType];

        foreach ( var exclusion in exclusions )
        {
            collector.AddExclusion( exclusion );
        }

        // Process attributes in parallel.
        var attributes = collector.Compilation.GetAllAttributesOfType( aspectType );

        return this._concurrentTaskRunner.RunConcurrentlyAsync( attributes, ProcessAttribute, collector.CancellationToken );

        void ProcessAttribute( IAttribute attribute )
        {
            collector.CancellationToken.ThrowIfCancellationRequested();

            var attributeData = attribute.GetAttributeData();

            if ( attributeDeserializer.TryCreateAttribute( attributeData, collector.Diagnostics, out var attributeInstance ) )
            {
                var targetDeclaration = attribute.ContainingDeclaration;

                var aspectInstance = aspectClass.CreateAspectInstanceFromAttribute(
                    (IAspect) attributeInstance,
                    targetDeclaration,
                    attribute );

                var eligibility = aspectInstance.ComputeEligibility( targetDeclaration );

                if ( eligibility == EligibleScenarios.None )
                {
                    var requestedEligibility = aspectInstance.IsInheritable ? EligibleScenarios.Inheritance : EligibleScenarios.Default;

                    var reason = aspectClass.GetIneligibilityJustification(
                        requestedEligibility,
                        new DescribedObject<IDeclaration>( targetDeclaration ) )!;

                    collector.Diagnostics.Report(
                        GeneralDiagnosticDescriptors.AspectNotEligibleOnTarget.CreateRoslynDiagnostic(
                            attribute.GetDiagnosticLocation(),
                            (aspectClass.ShortName, targetDeclaration.DeclarationKind, targetDeclaration, reason) ) );
                }

                collector.AddAspectInstance( aspectInstance );
            }
        }
    }

    public ContributorKind ContributorKind => ContributorKind.AspectSource;
}