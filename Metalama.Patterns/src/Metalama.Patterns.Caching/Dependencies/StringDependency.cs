// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Dependencies;

/// <summary>
/// A cache dependency that is already represented as a string.
/// </summary>
[PublicAPI]
public sealed record StringDependency( string Key ) : ICacheDependency
{
    /// <param name="cachingService"></param>
    /// <inheritdoc />
    string ICacheDependency.GetCacheKey( ICachingService cachingService ) => this.Key;

    IReadOnlyCollection<ICacheDependency> ICacheDependency.CascadeDependencies => Array.Empty<ICacheDependency>();
}