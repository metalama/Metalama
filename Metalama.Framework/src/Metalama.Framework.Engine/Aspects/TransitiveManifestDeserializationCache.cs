// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// Caches the transitive manifests deserialized from the metadata of referenced assemblies.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TransitivePipelineContributorSource.Create"/> runs on every pipeline execution and walks every
/// reference. The manifest <em>bytes</em> of a <see cref="Microsoft.CodeAnalysis.PortableExecutableReference"/> are
/// already cached by <c>MetadataReader</c>, but deserializing them was not, so at design time every package
/// reference carrying a manifest was deserialized again on each run.
/// </para>
/// <para>
/// The cache is a project service, and that scope is load-bearing rather than incidental: a manifest is
/// deserialized with the <em>consuming</em> project's service provider so that it binds to that project's
/// compile-time copy of each type (issue #1710). Sharing one deserialized instance between projects would
/// reintroduce exactly the cross-copy merge this fixes, so entries must never escape the project they were
/// deserialized for.
/// </para>
/// <para>
/// A package reference is keyed by assembly path and last-write time, which is how <c>MetadataReader</c> already
/// decides its own bytes are stale. A project reference has no file, so it is keyed by the producing assembly and
/// the content hash of its manifest. Both are scoped to the consuming project's <see cref="CompileTimeProject"/>:
/// should the project be re-projected (its compile-time closure changed), previously deserialized manifests are
/// bound to the superseded copy, so the cache is dropped rather than reused.
/// </para>
/// </remarks>
internal sealed class TransitiveManifestDeserializationCache : IProjectService
{
    private readonly object _sync = new();

    private ConcurrentDictionary<(string Path, DateTime LastWrite), ITransitiveAspectsManifest> _manifests = new();
    private ConcurrentDictionary<(AssemblyIdentity Producer, long Hash), ITransitiveAspectsManifest> _manifestsByHash = new();
    private CompileTimeProject? _boundTo;

    /// <summary>
    /// Gets the manifest deserialized from the given assembly, calling <paramref name="deserialize"/> only when it
    /// is not already cached for <paramref name="consumerProject"/>.
    /// </summary>
    public ITransitiveAspectsManifest GetOrAdd(
        string path,
        DateTime lastWrite,
        CompileTimeProject? consumerProject,
        Func<ITransitiveAspectsManifest> deserialize )
    {
        // A null consumer project means we cannot tell which compile-time copy the result would be bound to, so we
        // do not cache at all rather than risk handing it to a differently bound consumer.
        if ( consumerProject == null )
        {
            return deserialize();
        }

        this.EnsureBoundTo( consumerProject );

        return this._manifests.GetOrAdd( (path, lastWrite), _ => deserialize() );
    }

    /// <summary>
    /// Gets the manifest deserialized from the given bytes, calling <paramref name="deserialize"/> only when a
    /// manifest with the same content is not already cached for <paramref name="consumerProject"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This serves project references, which have no file to key on. The key pairs the producing assembly with the
    /// content hash, so an entry means "this project's manifest, at this content". Including the producer is not
    /// what makes the key safe, since a hash collision is already discounted at this scale (see the remarks on
    /// <see cref="SerializedTransitiveAspectManifest"/>); it is what makes the key say what it means, and it keeps
    /// two projects that happen to emit identical manifests in separate entries.
    /// </para>
    /// <para>
    /// The property that actually prevents a manifest reaching a consumer it was not deserialized for is the
    /// scoping of this cache to the consuming project, not the producer in the key.
    /// </para>
    /// </remarks>
    public ITransitiveAspectsManifest GetOrAdd(
        AssemblyIdentity producer,
        in SerializedTransitiveAspectManifest serialized,
        CompileTimeProject? consumerProject,
        Func<ITransitiveAspectsManifest> deserialize )
    {
        if ( consumerProject == null || serialized.IsDefaultOrEmpty )
        {
            return deserialize();
        }

        this.EnsureBoundTo( consumerProject );

        return this._manifestsByHash.GetOrAdd( (producer, serialized.Hash), _ => deserialize() );
    }

    private void EnsureBoundTo( CompileTimeProject consumerProject )
    {
        lock ( this._sync )
        {
            if ( !ReferenceEquals( this._boundTo, consumerProject ) )
            {
                this._boundTo = consumerProject;
                this._manifests = new ConcurrentDictionary<(string, DateTime), ITransitiveAspectsManifest>();

                this._manifestsByHash = new ConcurrentDictionary<(AssemblyIdentity, long), ITransitiveAspectsManifest>();
            }
        }
    }
}
