// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Aspects;

namespace Metalama.Framework.DesignTime.Pipeline;

/// <summary>
/// Associates a <see cref="ProjectKey"/> and a <see cref="TransitiveAspectsManifest"/>.
/// </summary>
internal readonly struct DesignTimeProjectReference
{
    // The two manifest representations below are always both set or both null: a reference carries them when it is
    // a Metalama project that exports something to inherit (an inheritable aspect, option, annotation, or validator),
    // and carries neither when it is not a Metalama project or is one that exports nothing (see gate in
    // DesignTimeAspectPipelineResult.HasTransitiveAspectManifestContent). They are not interchangeable: they serve
    // different consumers and neither can be derived from the other at the point where it is needed.

    /// <summary>
    /// Gets the referenced project's live manifest. Used only by <see cref="DesignTimeProjectVersion.ReferencedExtensions"/>,
    /// which needs the concrete <c>DesignTimeAspectPipelineResult</c> to read its design-time extension collections
    /// (a shape the serialized manifest does not carry).
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
    public SerializedTransitiveAspectManifest SerializedTransitiveAspectManifest { get; }

    public ProjectKey ProjectKey { get; }

    public DesignTimeProjectReference(
        ProjectKey projectKey,
        ITransitiveAspectsManifest? transitiveAspectsManifest = null,
        SerializedTransitiveAspectManifest serializedTransitiveAspectManifest = default )
    {
        // Both manifest representations are either both present or both absent: a reference either has a
        // transitive aspect manifest (in both its live and its serialized form) or is not a Metalama project.
        Invariant.Assert( transitiveAspectsManifest != null == !serializedTransitiveAspectManifest.IsDefaultOrEmpty );

        this.TransitiveAspectsManifest = transitiveAspectsManifest;
        this.SerializedTransitiveAspectManifest = serializedTransitiveAspectManifest;
        this.ProjectKey = projectKey;
    }

    // Comparing two references is not supported, and the overrides below exist to stop the default value equality
    // of a struct from silently doing it anyway.
    //
    // The only reason to compare these is change detection, and a whole-struct equality is wrong for that.
    // TransitiveAspectsManifest is a DesignTimeAspectPipelineResult, a class with no value equality, so comparing it
    // is comparing references; every pipeline run produces a new result, so the comparison would report "different"
    // even when the exported surface is byte-identical.
    //
    // A caller that needs to know whether a reference's manifest changed should compare
    // SerializedTransitiveAspectManifest instead, which compares by content hash.
    //
    // Nothing compares these today: an earlier implementation compared the manifest by reference and was unused,
    // which the compiler confirmed by reporting its precomputed hash code as dead.

    private const string _comparisonNotSupported =
        "Comparing " + nameof(DesignTimeProjectReference) + " is not supported, because it would compare the live "
        + "transitive manifest by reference and so report a change on every pipeline run. To compare content, "
        + "compare the " + nameof(SerializedTransitiveAspectManifest) + " values instead.";

    public override bool Equals( object? obj ) => throw new NotSupportedException( _comparisonNotSupported );

    public override int GetHashCode() => throw new NotSupportedException( _comparisonNotSupported );
}