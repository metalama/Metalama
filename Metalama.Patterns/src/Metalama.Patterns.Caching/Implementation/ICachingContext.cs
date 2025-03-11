// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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