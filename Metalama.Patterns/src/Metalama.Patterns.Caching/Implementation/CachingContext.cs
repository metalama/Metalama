// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Patterns.Caching.Implementation;

internal sealed class CachingContext : IDisposable, ICachingContext
{
    private static readonly AsyncLocal<ICachingContext?> _currentContext = new();

    [AllowNull]
    public static ICachingContext Current
    {
        get => _currentContext.Value ??= NullCachingContext.Instance;
        internal set => _currentContext.Value = value;
    }

    private readonly string _key;
    private readonly object _dependenciesSync = new();

    private bool _disposed;
    private HashSet<string>? _dependencies;
    private ImmutableHashSet<string>? _immutableDependencies;

    private CachingContext( string key, CachingContextKind options, ICachingContext parent )
    {
        this._key = key;
        this.Kind = options;
        this.Parent = parent;
    }

    public CachingContextKind Kind { get; }

    internal ImmutableHashSet<string> Dependencies
    {
        get
        {
            if ( this._immutableDependencies == null )
            {
                lock ( this._dependenciesSync )
                {
                    this._immutableDependencies =
                        this._dependencies == null
                            ? ImmutableHashSet<string>.Empty
                            : this._dependencies.ToImmutableHashSet();
                }
            }

            return this._immutableDependencies;
        }
    }

    [UsedImplicitly]
    public ICachingContext? Parent { get; }

    ICachingContext? ICachingContext.Parent => this.Parent;

    internal static CachingContext OpenRefreshContext( string key )
    {
        var context = new CachingContext( key, CachingContextKind.Refresh, Current );
        Current = context;

        return context;
    }

    internal static CachingContext OpenCacheContext( string key )
    {
        var context = new CachingContext( key, CachingContextKind.Cache, Current );
        Current = context;

        return context;
    }

    internal static SuspendedCachingContext OpenSuspendedCacheContext()
    {
        var context = new SuspendedCachingContext( Current );
        Current = context;

        return context;
    }

    [MemberNotNull( nameof(_dependencies) )]
    private void PrepareAddDependency()
    {
        this._dependencies ??= new HashSet<string>();
        this._immutableDependencies = null;
    }

    public void AddDependency( string key )
    {
        if ( string.IsNullOrEmpty( this._key ) )
        {
            throw new InvalidOperationException( "This method can be invoked only in the context of a cached method." );
        }

        if ( this._disposed )
        {
            this.Parent?.AddDependency( key );

            return;
        }

        lock ( this._dependenciesSync )
        {
            this.PrepareAddDependency();
            this._dependencies.Add( key );
        }
    }

    public void AddDependencies( IEnumerable<string> dependencies )
    {
        if ( string.IsNullOrEmpty( this._key ) )
        {
            throw new InvalidOperationException( "This method can be invoked only in the context of a cached method." );
        }

        if ( this._disposed )
        {
            this.Parent?.AddDependencies( dependencies );

            return;
        }

        lock ( this._dependenciesSync )
        {
            this.PrepareAddDependency();

            foreach ( var dependency in dependencies )
            {
                this._dependencies.Add( dependency );
            }
        }
    }

    internal void AddDependenciesToParent( CachingBackend backend )
    {
        if ( this.Parent != null )
        {
            this.Parent.AddDependencies( this.Dependencies );

            if ( backend.SupportedFeatures.Dependencies )
            {
                this.Parent.AddDependency( this._key );
            }
        }
    }

    public void Dispose()
    {
        if ( Current != this )
        {
            throw new InvalidOperationException( "Only the current context can be disposed." );
        }

        this._disposed = true;

        Current = this.Parent;
    }
}