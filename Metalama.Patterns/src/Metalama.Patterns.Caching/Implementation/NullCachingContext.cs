// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Implementation;

[Serializable]
internal sealed class NullCachingContext : MarshalByRefObject, ICachingContext
{
    private NullCachingContext() { }

    public static NullCachingContext Instance { get; } = new();

    public ICachingContext? Parent => null;

    public void AddDependencies( IEnumerable<string>? dependencies ) { }

    public void AddDependency( string dependency ) { }

    public CachingContextKind Kind => CachingContextKind.None;
}