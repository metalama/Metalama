// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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