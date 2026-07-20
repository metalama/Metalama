// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Aspects
{
    public interface ITransitiveAspectManifestProvider : IProjectService
    {
        /// <summary>
        /// Attempts to get a referenced project's live transitive manifest, which carries the same content as
        /// <see cref="GetSerializedTransitiveAspectsManifest"/> but as the producer's in-memory object, together
        /// with the producer's <see cref="AspectPipelineConfiguration"/>. A consumer can consume the returned
        /// manifest directly and skip deserialization when the producer's compile-time copies match its own (issue
        /// #1710 fast path); otherwise it must deserialize, which rebinds every option and aspect to the consumer's
        /// own copy. Returns <c>false</c> when there is no reusable manifest, i.e. the reference is not a
        /// current-version Metalama project, or exports nothing to inherit.
        /// </summary>
        bool TryGetReusableTransitiveAspectsManifest(
            Compilation compilationReferenceCompilation,
            [NotNullWhen( true )] out ITransitiveAspectsManifest? manifest,
            [NotNullWhen( true )] out AspectPipelineConfiguration? producerConfiguration );

        /// <summary>
        /// Gets the transitive aspect manifest of a referenced project in its serialized form. That form is
        /// compilation-neutral by definition: compile-time types are always written as their run-time names. It is
        /// produced with the <em>referenced</em> project's service provider, not because that determines the
        /// stored names, but because only that project's closure can name (resolve) its own compile-time copy of a
        /// shared assembly. The consumer deserializes it with its own service provider, which binds the run-time
        /// names to the consumer's own compile-time copy of each type, so inherited aspects and options are bound
        /// to the consuming project's copy of a shared (e.g. multi-targeted) compile-time assembly rather than the
        /// producer's copy (issue #1710). Returns <c>null</c> if the reference has no transitive aspect manifest,
        /// for instance because it is not a Metalama project or exports nothing to inherit. The returned value
        /// carries a hash of the bytes, so a consumer can tell an unchanged manifest from a merely re-produced one.
        /// </summary>
        SerializedTransitiveAspectManifest? GetSerializedTransitiveAspectsManifest( Compilation compilationReferenceCompilation );
    }
}