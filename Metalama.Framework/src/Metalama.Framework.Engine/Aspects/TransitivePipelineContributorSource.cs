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
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Options;
using Microsoft.CodeAnalysis;
using System;
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

    private TransitivePipelineContributorSource( ImmutableDictionary<AssemblyIdentity, ITransitiveAspectsManifest> manifests, ImmutableArray<IPipelineContributor> contributors )
    {
        this._manifests = manifests;
        this.Contributors = contributors;
    }

    public static TransitivePipelineContributorSource Create(
        CompilationContext compilationContext,
        AspectClassCollection aspectClasses,
        ProjectServiceProvider serviceProvider,
        UserDiagnosticSink diagnosticSink )
    {
        var pipelineExtensions = serviceProvider.GetRequiredService<PipelineExtensionProvider>().Extensions;

        var inheritableAspectProvider = serviceProvider.GetService<ITransitiveAspectManifestProvider>();

        var inheritedAspectsBuilder = ImmutableDictionaryOfArray<IAspectClass, InheritableAspectInstance>.CreateBuilder();
        var contributorsBuilder = ImmutableArray.CreateBuilder<IPipelineContributor>();
        var manifestDictionaryBuilder = ImmutableDictionary.CreateBuilder<AssemblyIdentity, ITransitiveAspectsManifest>();

        var aspectClassesByName = aspectClasses.Dictionary;

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
                            try
                            {
                                manifest = TransitiveAspectsManifest.Deserialize(
                                    new MemoryStream( bytes ),
                                    serviceProvider,
                                    compilationContext,
                                    filePath );
                            }
                            catch ( InvalidOperationException ex ) when ( ex.Message.Contains( "Could not locate assembly", StringComparison.Ordinal ) )
                            {
                                // This can happen when a referenced assembly uses PrivateAssets="all" on an aspect package,
                                // so the aspect assembly doesn't flow transitively to the consumer. In that case, we skip
                                // the transitive aspects manifest since the consumer doesn't have the required assemblies.
                                serviceProvider.GetLoggerFactory()
                                    .GetLogger( nameof(TransitivePipelineContributorSource) )
                                    .Warning?.Log(
                                        $"Cannot deserialize the transitive aspects manifest from '{filePath}': {ex.Message}. " +
                                        "This may be because the assembly was referenced with PrivateAssets=\"all\" in a dependency. Skipping." );

                                break;
                            }

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
                        // This can happen when the referenced assembly was compiled with a different version of Metalama
                        // that had a different set of aspect classes. We skip the unknown aspect class and continue.
                        serviceProvider.GetLoggerFactory()
                            .GetLogger( nameof(TransitivePipelineContributorSource) )
                            .Warning?.Log( $"Cannot find the aspect class '{aspectClassName}'." );

                        continue;
                    }

                    var targets = manifest.GetInheritableAspects( aspectClassName )
                        .WhereNotNull();

                    inheritedAspectsBuilder.AddRange( aspectClass, targets );
                }

                // Process manifest extensions.
                foreach ( var extension in pipelineExtensions )
                {
                    foreach ( var contributor in extension.GetPipelineContributorsFromTransitiveManifest( manifest.Extensions, aspectClasses, diagnosticSink ) )
                    {
                        contributorsBuilder.Add( contributor );
                    }
                }
            }
        }

        contributorsBuilder.Add( new InheritedAspectSourceImpl( serviceProvider, inheritedAspectsBuilder.ToImmutable() ) );
        
        return new TransitivePipelineContributorSource( manifestDictionaryBuilder.ToImmutable(), contributorsBuilder.ToImmutable() );
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