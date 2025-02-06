// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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