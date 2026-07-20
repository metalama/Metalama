// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Pipeline;

/// <summary>
/// Extends <see cref="ITransitiveCompilationResult"/> with a content hash of
/// <see cref="ITransitiveCompilationResult.Manifest"/>, so that a consumer can tell an unchanged manifest from a
/// changed one without hashing the bytes itself.
/// </summary>
/// <remarks>
/// <para>
/// A separate interface rather than a member on <see cref="ITransitiveCompilationResult"/>, because the producer is
/// a different, possibly older, version of Metalama: one that predates this interface returns a result that does not
/// implement it. A consumer must therefore type-test and fall back to hashing the bytes, which is what the added
/// member saves rather than enables.
/// </para>
/// </remarks>
[ComImport]
[Guid( "D4A3DD08-8127-40EB-B571-293BE58C2EBF" )]
public interface ITransitiveCompilationResult2 : ITransitiveCompilationResult
{
    /// <summary>
    /// Gets a content hash of <see cref="ITransitiveCompilationResult.Manifest"/>, or zero when there is no manifest.
    /// Producers must hash the same bytes they expose, so that equal hashes mean equal manifests.
    /// </summary>
    long ManifestHash { get; }
}
