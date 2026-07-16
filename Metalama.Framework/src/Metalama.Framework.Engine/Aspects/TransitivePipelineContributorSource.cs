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

    public bool ReferencesContainInitializableTypes { get; }

    private readonly ImmutableDictionary<AssemblyIdentity, ITransitiveAspectsManifest> _manifests;

    private TransitivePipelineContributorSource(
        ImmutableDictionary<AssemblyIdentity, ITransitiveAspectsManifest> manifests,
        ImmutableArray<IPipelineContributor> contributors )
    {
        this._manifests = manifests;
        this.Contributors = contributors;
        this.ReferencesContainInitializableTypes = manifests.Values.Any( m => m.ContainsInitializableTypes );
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
                            assemblyIdentity = metadataInfo.AssemblyIdentity;

                            // Deserialize the referenced assembly's manifest into THIS (consuming) project's
                            // compile-time copy of each type. The manifest stores run-time type names; deserializing
                            // with the current serviceProvider resolves them through the consumer's project closure,
                            // so both the inherited aspect and its options bind to the consumer's copy of shared
                            // (e.g. multi-targeted) compile-time assemblies (issue #1710). The consumer's closure
                            // already contains the canonical upstream projection (issue #1611's upstream-project
                            // reuse), so the inherited aspect's type still matches the consumer's IAspectClass.Type.
                            manifest = TransitiveAspectsManifest.Deserialize(
                                new MemoryStream( bytes ),
                                serviceProvider,
                                compilationContext,
                                filePath );
                        }
                    }

                    break;

                case CompilationReference compilationReference:
                    assemblyIdentity = compilationReference.Compilation.Assembly.Identity;

                    var serializedManifest =
                        inheritableAspectProvider?.GetSerializedTransitiveAspectsManifest( compilationReference.Compilation ) ?? default;

                    if ( !serializedManifest.IsDefaultOrEmpty )
                    {
                        // Deserialize the referenced project's manifest into THIS (consuming) project's compile-time
                        // copy of shared types. The bytes were serialized with the referenced project's service
                        // provider (run-time type names); deserializing with the current serviceProvider resolves
                        // those names to the consumer's own compile-time copy, so inherited aspects and options are
                        // bound to the consumer's copy and merge/cast correctly against the consumer's own options
                        // (issue #1710).
                        manifest = TransitiveAspectsManifest.Deserialize(
                            new MemoryStream( serializedManifest.ToArray() ),
                            serviceProvider,
                            compilationContext,
                            compilationReference.Compilation.AssemblyName );
                    }

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