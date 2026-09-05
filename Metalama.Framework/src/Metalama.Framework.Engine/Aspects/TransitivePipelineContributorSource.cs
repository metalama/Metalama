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
using Metalama.Framework.Engine.Pipeline;
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

        var inheritedAspects = new DictionaryOfList<IAspectClass, InheritableAspectInstance>();
        var contributorsBuilder = ImmutableArray.CreateBuilder<IPipelineContributor>();
        var manifestDictionaryBuilder = ImmutableDictionary.CreateBuilder<AssemblyIdentity, ITransitiveAspectsManifest>();

        var aspectClassesByName = aspectClasses.Dictionary;

        // Both are resolved once for the whole walk: the consuming project's compile-time project scopes the
        // deserialization cache below and decides whether a producer's live manifest can be reused.
        var consumerProject = serviceProvider.GetService<CompileTimeProjectRepository>()?.RootProject;
        var deserializationCache = serviceProvider.GetService<TransitiveManifestDeserializationCache>();

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
                            //
                            // The result is cached per consuming project, because this method runs on every pipeline
                            // execution while the referenced assembly rarely changes. The cache is keyed by path and
                            // last-write time, and is scoped to the consumer, since the manifest is bound to the
                            // consumer's compile-time copy and must not be shared with a differently bound project.
                            ITransitiveAspectsManifest Deserialize()
                                => TransitiveAspectsManifest.Deserialize( new MemoryStream( bytes ), serviceProvider, filePath );

                            manifest = deserializationCache == null
                                ? Deserialize()
                                : deserializationCache.GetOrAdd( filePath, metadataInfo.LastFileWrite, consumerProject, Deserialize );
                        }
                    }

                    break;

                case CompilationReference compilationReference:
                    assemblyIdentity = compilationReference.Compilation.Assembly.Identity;

                    // Fast path (issue #1710): when the referenced project's compile-time copies match ours, its live
                    // manifest objects are already instances of the very same compile-time types we use, so we can
                    // consume them directly and skip the serialize/deserialize round-trip.
                    if ( inheritableAspectProvider != null
                         && inheritableAspectProvider.TryGetReusableTransitiveAspectsManifest(
                             compilationReference.Compilation,
                             out var reusableManifest,
                             out var producerConfiguration )
                         && CanReuseLiveManifest( producerConfiguration, serviceProvider ) )
                    {
                        manifest = reusableManifest;
                    }
                    else if ( inheritableAspectProvider != null )
                    {
                        // The copies differ (e.g. a multi-targeted assembly built per TFM), so we cannot reuse the
                        // producer's objects. Deserialize the referenced project's manifest into THIS (consuming)
                        // project's compile-time copy of shared types. The bytes were serialized with the referenced
                        // project's service provider (run-time type names); deserializing with the current
                        // serviceProvider resolves those names to the consumer's own compile-time copy, so inherited
                        // aspects and options are bound to the consumer's copy and merge/cast correctly against the
                        // consumer's own options (issue #1710).
                        var serializedManifest =
                            inheritableAspectProvider.GetSerializedTransitiveAspectsManifest( compilationReference.Compilation );

                        if ( serializedManifest != null )
                        {
                            ITransitiveAspectsManifest DeserializeProjectManifest()
                                => TransitiveAspectsManifest.Deserialize(
                                    new MemoryStream( serializedManifest.Bytes.ToArray() ),
                                    serviceProvider,
                                    compilationReference.Compilation.AssemblyName );

                            // Keyed by the content hash rather than by the producing result, so that a producer
                            // edit which leaves the exported surface untouched, the common case, does not force a
                            // deserialization here.
                            manifest = deserializationCache == null
                                ? DeserializeProjectManifest()
                                : deserializationCache.GetOrAdd(
                                    assemblyIdentity.AssertNotNull(),
                                    serializedManifest,
                                    consumerProject,
                                    DeserializeProjectManifest );
                        }
                    }

                    break;

                case PortableExecutableReference { FilePath: null }:
                    // The transitive aspect manifest of a reference is read from the file of the referenced assembly,
                    // so it cannot be read for a reference whose metadata is not backed by a file. This is the same
                    // reference, and the same decision, as in CompileTimeProjectRepository.Builder
                    // .TryGetCompileTimeProject, whose comment states why the skip loses nothing: the reference is
                    // Roslyn's metadata-only "skeleton" for a project reference that crosses a language boundary, and a
                    // project of another language than the consuming C# one carries no Metalama compile-time code and
                    // therefore no inheritable aspect. Failing here would abort the execution of the pipeline for the
                    // whole project, and does so on every execution rather than only on initialization. See #1960.
                    serviceProvider.GetLoggerFactory()
                        .GetLogger( nameof(TransitivePipelineContributorSource) )
                        .Trace?.Log(
                            $"The reference '{reference.Display}' has no file path, so its transitive aspect manifest cannot be read. The reference is skipped." );

                    continue;

                default:
                    throw new AssertionFailedException( $"Unexpected reference kind: {reference}." );
            }

            // Process the manifest.
            if ( manifest != null )
            {
                var identity = assemblyIdentity.AssertNotNull();

                if ( manifestDictionaryBuilder.ContainsKey( identity ) )
                {
                    // Two references of the compilation have the same assembly identity, which happens when the same
                    // library reaches the project through two routes, for instance as a package and as a project
                    // reference (issue #1743). We silently ignore the duplicate rather than abort the whole pipeline:
                    // manifests are looked up by assembly identity, so a second entry could never be reached, and
                    // processing its aspects again would apply every inherited aspect of that assembly twice.
                    //
                    // Two distinct projects of one solution that produce the same assembly name and version reach this
                    // too, which is what Standalone/Issue1749.SameAssemblyIdentity and its design-time twin cover.
                    continue;
                }

                manifestDictionaryBuilder.Add( identity, manifest );

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

                    inheritedAspects.AddRange( aspectClass, targets );
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

        contributorsBuilder.Add( new InheritedAspectSourceImpl( serviceProvider, inheritedAspects.Freeze() ) );

        return new TransitivePipelineContributorSource( manifestDictionaryBuilder.ToImmutable(), contributorsBuilder.ToImmutable() );
    }

    /// <summary>
    /// Determines whether the producer's live manifest can be consumed as-is instead of being deserialized into the
    /// consumer's compile-time copy. It is safe if and only if, for every run-time assembly the manifest could
    /// reference, the consumer resolves that assembly to the exact same compile-time (<c>ml!</c>) copy the producer
    /// used, so the producer's option and aspect objects are instances of the very CLR types the consumer uses for
    /// its own defaults, and no merge crosses copies.
    /// </summary>
    /// <remarks>
    /// The crucial subtlety (issue #1710) is that the consumer's compile-time closure may contain <em>several</em>
    /// copies of the same run-time assembly: a multi-targeted library whose per-TFM projections are both loaded, for
    /// example the <c>net472</c> copy the consumer references directly and the <c>netstandard2.0</c> copy pulled in
    /// through a project reference. When a run-time assembly the producer touches is ambiguous like that on the
    /// consumer side, we cannot know the producer's objects match the copy the consumer uses for its own defaults,
    /// so we must take the safe path and deserialize. This test is coarse, since it also declines when the ambiguity
    /// is in an assembly the manifest never touches, but it never accepts an unsafe reuse.
    /// </remarks>
    internal static bool CanReuseLiveManifest( AspectPipelineConfiguration producerConfiguration, ProjectServiceProvider consumerServiceProvider )
    {
        var producerProject = producerConfiguration.CompileTimeProject;

        if ( producerProject == null )
        {
            return false;
        }

        var consumerProject = consumerServiceProvider.GetService<CompileTimeProjectRepository>()?.RootProject;

        if ( consumerProject == null )
        {
            return false;
        }

        // Reference-identical CLR types require the same domain (assembly load context). Without it, even matching
        // ml! names denote distinct types in distinct load contexts, so the producer's objects could not be merged.
        if ( !ReferenceEquals( producerProject.Domain, consumerProject.Domain ) )
        {
            return false;
        }

        // The consumer must resolve every run-time assembly the producer's closure covers to exactly one compile-time
        // copy, and that copy must be the producer's. More than one entry means the consumer holds several copies of
        // that assembly (the multi-targeted case above), so we cannot tell which one its own defaults use. (The ml!
        // name's hash already folds in the transitive reference identities, so name equality implies the same source
        // and reference closure, i.e. the same physical copy.) The grouping is memoized on the consumer's project,
        // so it is built once per project rather than once per reference.
        var consumerProjectsByRunTimeName = consumerProject.ClosureProjectsGroupedByRunTimeAssemblyName;

        foreach ( var producerClosureProject in producerProject.ClosureProjects )
        {
            if ( consumerProjectsByRunTimeName[producerClosureProject.RunTimeIdentity.Name] is not [var consumerCopy]
                 || !string.Equals(
                     consumerCopy.CompileTimeIdentity.Name,
                     producerClosureProject.CompileTimeIdentity.Name,
                     StringComparison.Ordinal ) )
            {
                return false;
            }
        }

        return true;
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