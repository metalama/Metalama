// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Aspects;

namespace Metalama.Framework.DesignTime.Pipeline;

/// <summary>
/// A referenced project, as seen by the referencing project's design-time pipeline: a <see cref="ProjectKey"/>, the
/// serialized transitive manifest when the reference exports anything to inherit, and, for a reference built against
/// the same version of Metalama, that project's live pipeline result as well.
/// </summary>
internal readonly struct DesignTimeProjectReference
{
    // A reference carries a serialized manifest when it is a Metalama project that exports something to inherit (an
    // inheritable aspect, option, annotation, or validator), and carries none when it is not a Metalama project or is
    // one that exports nothing (see gate in DesignTimeAspectPipelineResult.HasTransitiveAspectManifestContent).
    //
    // The live result below is carried in addition, but only for a same-version reference. The two are not
    // interchangeable: they serve different consumers and neither can be derived from the other at the point where
    // it is needed.

    /// <summary>
    /// Gets the referenced project's live pipeline result, or <c>null</c> when the reference is to a project built
    /// against a different version of Metalama. Used only by
    /// <see cref="DesignTimeProjectVersion.ReferencedExtensions"/> and
    /// <see cref="DesignTimeProjectVersion.TryGetReusableTransitiveAspectsManifest"/>, both of which need the
    /// concrete <see cref="DesignTimeAspectPipelineResult"/>: the first to read its design-time extension
    /// collections (a shape the serialized manifest does not carry), the second to reuse the producer's live objects
    /// and its configuration.
    /// </summary>
    /// <remarks>
    /// This is deliberately typed as the concrete result rather than <c>ITransitiveAspectsManifest</c>. A
    /// cross-version producer cannot supply one at all: its pipeline result is an object of the *other* version's
    /// <c>Metalama.Framework.Engine</c>, so the only thing that can cross is the serialized manifest. Typing it
    /// concretely states that, instead of accepting any manifest here and having both readers narrow to this type
    /// and silently ignore anything else.
    /// </remarks>
    public DesignTimeAspectPipelineResult? TransitiveAspectsManifest { get; }

    /// <summary>
    /// Gets the transitive aspect manifest in its serialized (compilation-neutral) form: compile-time types are
    /// written as their run-time names. Used by the engine, which deserializes it with the <em>consuming</em>
    /// project's service provider so the run-time names bind to the consumer's compile-time copy of shared types
    /// (issue #1710). It must be serialized here, with the <em>referenced</em> project's service provider, because
    /// only that project's closure can name (resolve) its own compile-time copy of a shared assembly; the
    /// consuming project's provider could not serialize types bound to a copy that is not in its closure.
    /// </summary>
    public SerializedTransitiveAspectManifest? SerializedTransitiveAspectManifest { get; }

    public ProjectKey ProjectKey { get; }

    public DesignTimeProjectReference(
        ProjectKey projectKey,
        DesignTimeAspectPipelineResult? transitiveAspectsManifest = null,
        SerializedTransitiveAspectManifest? serializedTransitiveAspectManifest = null )
    {
        // A live result is only ever carried alongside a serialized manifest, never on its own: it comes from a
        // same-version reference that exports something to inherit, and such a reference always serializes too. The
        // converse does not hold, because a cross-version reference carries the serialized manifest alone.
        Invariant.Assert( transitiveAspectsManifest == null || serializedTransitiveAspectManifest != null );

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