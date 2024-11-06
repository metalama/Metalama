// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Fabrics;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Engine.Validation;
using Metalama.Framework.Options;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// An aspect source that applies aspects that are inherited from referenced assemblies or projects.
/// </summary>
internal sealed class TransitivePipelineContributorSource : IAspectSource, IValidatorSource, IExternalHierarchicalOptionsProvider, IExternalAnnotationProvider
{
    private readonly ImmutableDictionaryOfArray<IAspectClass, InheritableAspectInstance> _inheritedAspects;
    private readonly ImmutableArray<TransitiveValidatorInstance> _referenceValidators;
    private readonly ImmutableDictionary<AssemblyIdentity, ITransitiveAspectsManifest> _manifests;
    private readonly IConcurrentTaskRunner _concurrentTaskRunner;

    public TransitivePipelineContributorSource(
        CompilationContext compilationContext,
        ImmutableArray<IAspectClass> aspectClasses,
        ProjectServiceProvider serviceProvider )
    {
        var inheritableAspectProvider = serviceProvider.GetService<ITransitiveAspectManifestProvider>();
        this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();

        var inheritedAspectsBuilder = ImmutableDictionaryOfArray<IAspectClass, InheritableAspectInstance>.CreateBuilder();
        var validatorsBuilder = ImmutableArray.CreateBuilder<TransitiveValidatorInstance>();
        var manifestDictionaryBuilder = ImmutableDictionary.CreateBuilder<AssemblyIdentity, ITransitiveAspectsManifest>();

        var aspectClassesByName = aspectClasses.ToDictionary( t => t.FullName, t => t );

        foreach ( var reference in compilationContext.Compilation.References )
        {
            // Get the manifest of the reference.
            ITransitiveAspectsManifest? manifest = null;
            AssemblyIdentity? assemblyIdentity = null;

            switch ( reference )
            {
                case PortableExecutableReference { FilePath: { } filePath }:
                    if ( MetadataReader.TryGetMetadata( filePath, out var metadataInfo ) )
                    {
                        if ( metadataInfo.Resources.TryGetValue( CompileTimeConstants.InheritableAspectManifestResourceName, out var bytes ) )
                        {
                            manifest = TransitiveAspectsManifest.Deserialize(
                                new MemoryStream( bytes ),
                                serviceProvider,
                                compilationContext,
                                filePath );

                            assemblyIdentity = metadataInfo.AssemblyIdentity;
                        }
                    }

                    break;

                case CompilationReference compilationReference:
                    manifest = inheritableAspectProvider?.GetTransitiveAspectsManifest( compilationReference.Compilation );
                    assemblyIdentity = compilationReference.Compilation.Assembly.Identity;

                    break;

                default:
                    throw new AssertionFailedException( $"Unexpected reference kind: {reference}." );
            }

            // Process the manifest.
            if ( manifest != null )
            {
                manifestDictionaryBuilder.Add( assemblyIdentity.AssertNotNull(), manifest );

                // Process inherited aspects.
                foreach ( var aspectClassName in manifest.InheritableAspectTypes )
                {
                    if ( !aspectClassesByName.TryGetValue( aspectClassName, out var aspectClass ) )
                    {
                        // It seems to happen with inherited aspects at design time when the aspect class could not be found.
                        // In that case, an error should have been reported above. Anyway, this should not be the problem of the present
                        // method but of the code upstream and we should cope with that situation/
                        serviceProvider.GetLoggerFactory()
                            .GetLogger( nameof(TransitivePipelineContributorSource) )
                            .Warning?.Log( $"Cannot find the aspect class '{aspectClassesByName}'." );

                        continue;
                    }

                    var targets = manifest.GetInheritableAspects( aspectClassName )
                        .WhereNotNull();

                    inheritedAspectsBuilder.AddRange( aspectClass, targets );
                }

                // Process validators.
                validatorsBuilder.AddRange( manifest.ReferenceValidators );
            }
        }

        this._inheritedAspects = inheritedAspectsBuilder.ToImmutable();
        this._referenceValidators = validatorsBuilder.ToImmutable();
        this._manifests = manifestDictionaryBuilder.ToImmutable();
    }

    public ImmutableArray<IAspectClass> AspectClasses => this._inheritedAspects.Keys.ToImmutableArray();

    public Task CollectAspectInstancesAsync(
        IAspectClass aspectClass,
        OutboundActionCollectionContext context )
    {
        return this._concurrentTaskRunner.RunConcurrentlyAsync( this._inheritedAspects[aspectClass], ProcessAspectInstance, context.CancellationToken );

        void ProcessAspectInstance( InheritableAspectInstance inheritedAspectInstance )
        {
            var baseDeclaration = inheritedAspectInstance.TargetDeclaration.GetTargetOrNull( context.Compilation );

            if ( baseDeclaration == null )
            {
                return;
            }

            // We need to provide instances on the first level of derivation only because the caller will add to the next levels.

            foreach ( var derived in ((IDeclarationImpl) baseDeclaration).GetDerivedDeclarations( DerivedTypesOptions.DirectOnly ) )
            {
                context.Collector.AddAspectInstance(
                    new AspectInstance(
                        inheritedAspectInstance.Aspect,
                        derived,
                        (AspectClass) aspectClass,
                        new AspectPredecessor( AspectPredecessorKind.Inherited, inheritedAspectInstance ) ) );
            }
        }
    }

    Task IValidatorSource.CollectValidatorsAsync(
        ValidatorKind kind,
        CompilationModelVersion compilationModelVersion,
        OutboundActionCollectionContext context )
    {
        if ( kind == ValidatorKind.Reference )
        {
            foreach ( var validator in this._referenceValidators )
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var validationTarget = validator.ValidatedDeclaration?.GetTargetOrNull( context.Compilation );

                if ( validationTarget?.GetSymbol() == null )
                {
                    continue;
                }

                context.Collector.AddValidator(
                    new ReferenceValidatorInstance(
                        validationTarget,
                        validator.GetReferenceValidatorDriver(),
                        ValidatorImplementation.Create( validator.Object, validator.State ),
                        validator.ReferenceKinds,
                        validator.IncludeDerivedTypes,
                        validator.DiagnosticSourceDescription,
                        validator.Granularity ) );
            }
        }

        return Task.CompletedTask;
    }

    public IEnumerable<string> GetOptionTypes()
        => this._manifests.SelectMany( m => m.Value.InheritableOptions.Keys )
            .Select( x => x.OptionType )
            .Distinct();

    public bool TryGetOptions( IDeclaration declaration, string optionsType, [NotNullWhen( true )] out IHierarchicalOptions? options )
    {
        if ( this._manifests.TryGetValue( ((AssemblyIdentityModel) declaration.DeclaringAssembly.Identity).Identity, out var manifest ) )
        {
            return manifest.InheritableOptions.TryGetValue( new HierarchicalOptionsKey( optionsType, declaration.ToSerializableId() ), out options );
        }
        else
        {
            options = null;

            return false;
        }
    }

    public ImmutableArray<IAnnotation> GetAnnotations( IDeclaration declaration )
    {
        if ( this._manifests.TryGetValue( ((AssemblyIdentityModel) declaration.DeclaringAssembly.Identity).Identity, out var manifest ) )
        {
            return manifest.Annotations[declaration.ToSerializableId()];
        }
        else
        {
            return ImmutableArray<IAnnotation>.Empty;
        }
    }
}