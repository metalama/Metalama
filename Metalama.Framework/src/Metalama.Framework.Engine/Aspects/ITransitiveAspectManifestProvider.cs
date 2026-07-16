// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Aspects
{
    public interface ITransitiveAspectManifestProvider : IProjectService
    {
        /// <summary>
        /// Gets the transitive aspect manifest of a referenced project in its serialized form. That form is
        /// compilation-neutral by definition: compile-time types are always written as their run-time names. It is
        /// produced with the <em>referenced</em> project's service provider — not because that determines the
        /// stored names, but because only that project's closure can name (resolve) its own compile-time copy of a
        /// shared assembly. The consumer deserializes it with its own service provider, which binds the run-time
        /// names to the consumer's own compile-time copy of each type, so inherited aspects and options are bound
        /// to the consuming project's copy of a shared (e.g. multi-targeted) compile-time assembly rather than the
        /// producer's copy (issue #1710). Returns a default (or empty) array if the reference has no transitive
        /// aspect manifest (e.g. it is not a Metalama project).
        /// </summary>
        ImmutableArray<byte> GetSerializedTransitiveAspectsManifest( Compilation compilationReferenceCompilation );
    }
}