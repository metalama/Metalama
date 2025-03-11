// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Building;

/// <summary>
/// Arguments of the <see cref="ConcreteCachingBackendBuilder.CreateBackend"/> method.
/// </summary>
public sealed record CreateBackendArgs
{
    /// <summary>
    /// Gets the number of the cache layer, e.g. 1 for L1 or 2 for L2.
    /// </summary>
    public int Layer { get; internal init; }
}