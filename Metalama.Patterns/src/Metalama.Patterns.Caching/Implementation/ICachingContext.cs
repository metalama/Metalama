// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Represents the context in which a method being cached is executing. 
/// </summary>
[PublicAPI]
internal interface ICachingContext
{
    /// <summary>
    /// Gets the parent context.
    /// </summary>
    ICachingContext? Parent { get; }

    /// <summary>
    /// Gets the kind of <see cref="ICachingContext"/>.
    /// </summary>
    CachingContextKind Kind { get; }

    void AddDependency( string key );

    void AddDependencies( IEnumerable<string> key );
}