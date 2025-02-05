// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Testing.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

#pragma warning disable VSTHRD200

public sealed class CacheTests : UnitTestClass
{
    [Fact]
    public void Hit()
    {
        var cache = new TestCache( 10 );

        // Cache miss because of non-existent item.
        Assert.Equal( 0, cache.GetOrAdd( 0, _ => 0 ) );
        Assert.Equal( 0, cache.GetOrAdd( 1, _ => 0 ) );

        // Cache miss because of invalid predicate (1 is odd).
        Assert.Equal( 1, cache.GetOrAdd( 1, _ => 1 ) );

        // Cache hit.
        Assert.Equal( 0, cache.GetOrAdd( 0, _ => 2 ) );
    }

    [Fact]
    public void Recursion()
    {
        var cache = new TestCache( 10 );
        Assert.Equal( 1, cache.GetOrAdd( 0, _ => cache.GetOrAdd( 0, _ => 1 ) + 1 ) );
    }

    [Fact]
    public void Rotation()
    {
        const int capacity = 10;
        var cache = new TestCache( capacity );

        for ( var i = 0; i < capacity * 10; i++ )
        {
            cache.GetOrAdd( i, i1 => i1 );
        }

        Assert.True( cache.Count <= capacity );
    }

    [Fact]
    public async Task Concurrency()
    {
        var cache = new TestCache( 15 );

        // Test that the add method does not run concurrently.
        var locks = Enumerable.Range( 0, 20 ).ToDictionary( i => i, _ => new object() );

        var tasks = Enumerable.Range( 0, Environment.ProcessorCount )
            .Select(
                _ => Task.Run(
                    () =>
                    {
                        for ( var n = 0; n < 10; n++ )
                        {
                            foreach ( var pair in locks )
                            {
                                cache.GetOrAdd(
                                    pair.Key,
                                    k =>
                                    {
                                        // Here is the assertion: the Func should not be executed by two threads at a time.
                                        Assert.True( Monitor.TryEnter( pair.Value ) );
                                        Thread.Sleep( 1 );
                                        Monitor.Exit( pair.Value );

                                        return k;
                                    } );
                            }
                        }
                    } ) );

        await Task.WhenAll( tasks );
    }

    [Fact]
    public void FileBasedCache()
    {
        // This test does not tests eviction.

        var cache = new FileBasedCache<string>( TimeSpan.MaxValue );

        using var testContext = this.CreateTestContext();
        var directory = testContext.BaseDirectory;
        var fileName = Path.Combine( directory, "test.txt" );

        // Add to cache.
        File.WriteAllText( fileName, "1" );

        Assert.Equal( "1", cache.GetOrAdd( fileName, File.ReadAllText ) );

        // Cache hit.
        Assert.Equal( "1", cache.GetOrAdd( fileName, _ => "X" ) );

        // Wait more than the filesystem time resolution.
        Thread.Sleep( 10 );

        // Update the file.
        File.WriteAllText( fileName, "2" );

        // Cache miss. Re-read the file.
        Assert.Equal( "2", cache.GetOrAdd( fileName, File.ReadAllText ) );
    }

    private sealed class TestCache : Cache<int, int, int>
    {
        private readonly int _capacity;

        public TestCache( int capacity )
        {
            this._capacity = capacity;
        }

        protected override bool Validate( int key, in Item item ) => key % 2 == 0;

        protected override bool ShouldRotate() => this.RecentItemsCount >= this._capacity / 2;
    }
}