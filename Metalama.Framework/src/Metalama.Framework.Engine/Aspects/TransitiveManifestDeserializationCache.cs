// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Services;
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
/// Entries are keyed by assembly path and last-write time, which is how <c>MetadataReader</c> already decides its
/// own bytes are stale. They are additionally scoped to the consuming project's <see cref="CompileTimeProject"/>:
/// should the project be re-projected (its compile-time closure changed), previously deserialized manifests are
/// bound to the superseded copy, so the cache is dropped rather than reused.
/// </para>
/// </remarks>
internal sealed class TransitiveManifestDeserializationCache : IProjectService
{
    private readonly object _sync = new();

    private ConcurrentDictionary<(string Path, DateTime LastWrite), ITransitiveAspectsManifest> _manifests = new();
    private ConcurrentDictionary<long, ITransitiveAspectsManifest> _manifestsByHash = new();
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
    /// This serves project references, which have no file to key on. The key is the content hash, which
    /// <see cref="SerializedTransitiveAspectManifest"/> treats as an identity; see its remarks for why a collision
    /// is discounted at this scale.
    /// </remarks>
    public ITransitiveAspectsManifest GetOrAdd(
        in SerializedTransitiveAspectManifest serialized,
        CompileTimeProject? consumerProject,
        Func<ITransitiveAspectsManifest> deserialize )
    {
        if ( consumerProject == null || serialized.IsDefaultOrEmpty )
        {
            return deserialize();
        }

        this.EnsureBoundTo( consumerProject );

        return this._manifestsByHash.GetOrAdd( serialized.Hash, _ => deserialize() );
    }

    private void EnsureBoundTo( CompileTimeProject consumerProject )
    {
        lock ( this._sync )
        {
            if ( !ReferenceEquals( this._boundTo, consumerProject ) )
            {
                this._boundTo = consumerProject;
                this._manifests = new ConcurrentDictionary<(string, DateTime), ITransitiveAspectsManifest>();

                this._manifestsByHash = new ConcurrentDictionary<long, ITransitiveAspectsManifest>();
            }
        }
    }
}
