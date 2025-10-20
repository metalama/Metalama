// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Options;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// An aspect source that applies aspects that are inherited from referenced assemblies or projects.
/// </summary>
internal sealed partial class TransitivePipelineContributorSource : IExternalHierarchicalOptionsProvider, IExternalAnnotationProvider
{
    public ImmutableArray<IPipelineContributor> Contributors { get; }

    private readonly ImmutableDictionary<AssemblyIdentity, ITransitiveAspectsManifest> _manifests;

    public TransitivePipelineContributorSource(
        CompilationContext compilationContext,
        ImmutableArray<IAspectClass> aspectClasses,
        ProjectServiceProvider serviceProvider )
    {
        var pipelineExtensions = serviceProvider.GetRequiredService<PipelineExtensionProvider>().Extensions;

        var inheritableAspectProvider = serviceProvider.GetService<ITransitiveAspectManifestProvider>();

        var inheritedAspectsBuilder = ImmutableDictionaryOfArray<IAspectClass, InheritableAspectInstance>.CreateBuilder();
        var transitiveAspectsBuilder = ImmutableDictionaryOfArray<IAspectClass, TransitiveAspectInstance>.CreateBuilder();
        var contributorsBuilder = ImmutableArray.CreateBuilder<IPipelineContributor>();
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

                // Process transitive aspects.
                var transitiveAspects = manifest.Extensions.OfKind( ContributorKind.TransitiveAspect );
                transitiveAspectsBuilder.AddRange( transitiveAspects, a => a.AspectClass, a => a );

                // Process manifest extensions.
                foreach ( var extension in pipelineExtensions )
                {
                    foreach ( var contributor in extension.GetPipelineContributorsFromTransitiveManifest( manifest.Extensions ) )
                    {
                        contributorsBuilder.Add( contributor );
                    }
                }
            }
        }

        contributorsBuilder.Add( new InheritedAspectSourceImpl( serviceProvider, inheritedAspectsBuilder.ToImmutable() ) );
        contributorsBuilder.Add( new TransitiveAspectSourceImpl( transitiveAspectsBuilder.ToImmutable() ) );
        this.Contributors = contributorsBuilder.ToImmutable();
        this._manifests = manifestDictionaryBuilder.ToImmutable();
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