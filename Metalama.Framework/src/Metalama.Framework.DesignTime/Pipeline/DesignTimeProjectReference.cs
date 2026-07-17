// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Aspects;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline;

/// <summary>
/// Associates a <see cref="ProjectKey"/> and a <see cref="TransitiveAspectsManifest"/>.
/// </summary>
internal readonly struct DesignTimeProjectReference : IEquatable<DesignTimeProjectReference>
{
    // The two manifest representations below are always both set or both null (a reference either has a
    // transitive aspect manifest or is not a Metalama project). They are not interchangeable: they serve
    // different consumers and neither can be derived from the other at the point where it is needed.

    /// <summary>
    /// Gets the referenced project's live manifest. Used only by <see cref="DesignTimeProjectVersion.ReferencedExtensions"/>,
    /// which needs the concrete <c>DesignTimeAspectPipelineResult</c> to read its design-time extension collections
    /// — a shape the serialized manifest does not carry.
    /// </summary>
    public ITransitiveAspectsManifest? TransitiveAspectsManifest { get; }

    /// <summary>
    /// Gets the transitive aspect manifest in its serialized (compilation-neutral) form: compile-time types are
    /// written as their run-time names. Used by the engine, which deserializes it with the <em>consuming</em>
    /// project's service provider so the run-time names bind to the consumer's compile-time copy of shared types
    /// (issue #1710). It must be serialized here, with the <em>referenced</em> project's service provider, because
    /// only that project's closure can name (resolve) its own compile-time copy of a shared assembly; the
    /// consuming project's provider could not serialize types bound to a copy that is not in its closure.
    /// </summary>
    public ImmutableArray<byte> SerializedTransitiveAspectManifest { get; }

    public ProjectKey ProjectKey { get; }

    private readonly int _hashCode;

    public DesignTimeProjectReference(
        ProjectKey projectKey,
        ITransitiveAspectsManifest? transitiveAspectsManifest = null,
        ImmutableArray<byte> serializedTransitiveAspectManifest = default )
    {
        // Both manifest representations are either both present or both absent: a reference either has a
        // transitive aspect manifest (in both its live and its serialized form) or is not a Metalama project.
        Invariant.Assert( transitiveAspectsManifest != null == !serializedTransitiveAspectManifest.IsDefault );

        this.TransitiveAspectsManifest = transitiveAspectsManifest;
        this.SerializedTransitiveAspectManifest = serializedTransitiveAspectManifest;
        this.ProjectKey = projectKey;
        this._hashCode = HashCode.Combine( transitiveAspectsManifest, projectKey );
    }

    // The serialized manifest is not compared (nor hashed): it is derived from, and cached on, the same
    // DesignTimeAspectPipelineResult as TransitiveAspectsManifest, so equal TransitiveAspectsManifest implies
    // equal SerializedTransitiveAspectManifest.
    public bool Equals( DesignTimeProjectReference other )
        => Equals( this.TransitiveAspectsManifest, other.TransitiveAspectsManifest )
           && this.ProjectKey.Equals( other.ProjectKey );

    public override bool Equals( object? obj ) => obj is DesignTimeProjectReference other && this.Equals( other );

    public override int GetHashCode() => this._hashCode;
}