// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests;

public sealed class CacheKeyAspectTests : BaseCachingTests
{
    [Fact]
    public void Test()
    {
        using var context = this.InitializeTest( "CacheKeyAspectTests" );

        var c1 = new SomeClass( 1 );
        var c2 = new SomeClass( 2 );

        var i1 = GetId( c1 );
        var i2 = GetId( c2 );

        // We test that two instances return a distinct cache key.
        Assert.Equal( c1.Id, i1 );
        Assert.Equal( c2.Id, i2 );
    }

    [Cache]
    private static int GetId( SomeClass c ) => c.Id;

    private sealed class SomeClass
    {
        [CacheKey]
        public int Id { get; }

        public SomeClass( int id )
        {
            this.Id = id;
        }
    }

    public CacheKeyAspectTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }
}