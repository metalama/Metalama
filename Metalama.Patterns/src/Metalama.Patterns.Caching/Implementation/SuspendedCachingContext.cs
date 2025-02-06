// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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