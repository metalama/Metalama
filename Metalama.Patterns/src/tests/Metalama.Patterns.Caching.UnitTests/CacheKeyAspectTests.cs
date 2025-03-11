// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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