// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Dependencies;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests;

public sealed class DependencyTreeTests : BaseCachingTests
{
    public DependencyTreeTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }

    [Fact]
    public void TestParentDependencies()
    {
        using var context = this.InitializeTest( nameof(DependencyTreeTests) );

        var c = new CachedClass();
        var all1 = c.GetAll();
        var one1 = c.GetOne( 1 );
        c.UpdateOne( 1 );
        var all2 = c.GetAll();
        var one2 = c.GetOne( 1 );

        Assert.NotEqual( one1, one2 );

        // GetAll should also have been invalidated.
        Assert.NotEqual( all1, all2 );
    }

    private sealed class CachedClass
    {
        private int _counter;

        [Cache]
        public int GetAll()
        {
            CachingService.Default.AddDependency( new Dependency( "All" ) );

            return this._counter++;
        }

        public int GetOne( int x )
        {
            CachingService.Default.AddDependency( new Dependency( $"One:{x}", new Dependency( "All" ) ) );

            return this._counter++;
        }

        public void UpdateOne( int x )
        {
            this._counter++;

            CachingService.Default.Invalidate( new Dependency( $"One:{x}", new Dependency( "All" ) ) );
        }
    }

    private sealed class Dependency : ICacheDependency
    {
        private readonly string _key;

        public Dependency( string key, params ICacheDependency[] parents )
        {
            this._key = key;
            this.CascadeDependencies = parents;
        }

        public string GetCacheKey( ICachingService cachingService ) => this._key;

        public IReadOnlyCollection<ICacheDependency> CascadeDependencies { get; }
    }
}