// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Implementation;

internal sealed class SuspendedCachingContext : IDisposable, ICachingContext
{
    private readonly ICachingContext? _suspendedContext;

    internal SuspendedCachingContext( ICachingContext? suspendedContext )
    {
        this._suspendedContext = suspendedContext;
    }

    public ICachingContext? Parent => null;

    public void AddDependencies( IEnumerable<string>? dependencies ) { }

    public void AddDependency( string dependency ) { }

    public CachingContextKind Kind => CachingContextKind.None;

    public void Dispose()
    {
        if ( CachingContext.Current != this )
        {
            throw new InvalidOperationException( "Only the current context can be disposed." );
        }

        CachingContext.Current = this._suspendedContext;
    }
}