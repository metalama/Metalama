// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Distributed;

public abstract class BaseDistributedCacheTests : BaseCachingTests, IClassFixture<CachingClassFixture>
{
    private const int _timeout = 120000; // 2 minutes ought to be enough to anyone. (otherwise the test should be refactored, anyway).
    private static readonly TimeSpan _timeoutTimeSpan = TimeSpan.FromMilliseconds( _timeout * 0.8 );

    protected virtual bool TestDependencies { get; } = true;

    protected abstract Task<CachingBackend[]> CreateBackendsAsync();

    protected abstract CachingBackend[] CreateBackends();

    protected BaseDistributedCacheTests( CachingClassFixture cachingClassFixture, ITestOutputHelper testOutputHelper ) : base( testOutputHelper )
    {
        this.ClassFixture = cachingClassFixture;
    }

    /// <summary>
    /// If the actual class is a Redis test, connect to the persistent Redis server (or create the server, if it doesn't run yet).
    /// <para>
    /// If this is not a Redis test, do nothing.
    /// </para>
    /// </summary>
    protected virtual void ConnectToRedisIfRequired() { }

    private static async ValueTask DisposeAndVerifyBackendsAsync( IEnumerable<CachingBackend> backends, CancellationToken cancellationToken = default )
    {
        foreach ( var backend in backends )
        {
            await backend.DisposeAsync( cancellationToken );
            Assert.Equal( 0, backend.BackgroundTaskExceptions );
        }
    }

    private static void DisposeAndVerifyBackends( IEnumerable<CachingBackend> backends )
    {
        foreach ( var backend in backends )
        {
            backend.Dispose();
            Assert.Equal( 0, backend.BackgroundTaskExceptions );
        }
    }

    protected CachingClassFixture ClassFixture { get; }

    [Fact( Timeout = _timeout )]
    public async Task TestInvalidateDependencyIdenticalItemsAsync()
    {
        if ( !this.TestDependencies )
        {
            AssertEx.Inconclusive();

            return;
        }

        this.ConnectToRedisIfRequired();

        const string dependencyKey = "d", itemKey = "i";

        var backends = await this.CreateBackendsAsync();

        try
        {
            var itemRemovedEvents = new TaskCompletionSource<bool>[backends.Length];

            for ( var j = 0; j < backends.Length; j++ )
            {
                var index = j;
                backends[j].ItemRemoved += ( _, _ ) => itemRemovedEvents[index].TrySetResult( true );
            }

            for ( var j = 0; j < backends.Length; j++ )
            {
                var backend1 = backends[j];
                this.TestOutputHelper.WriteLine( $"Testing with master backend #{j}: {backend1}" );

                for ( var i = 0; i < backends.Length; i++ )
                {
                    itemRemovedEvents[i] = new TaskCompletionSource<bool>();
                    var backend2 = backends[i];
                    await backend2.SetItemAsync( itemKey, new CacheItem( "Hello, world.", ImmutableArray.Create( dependencyKey ) ) );
                }

                // [Porting] Not fixing, can't be certain of original intent.
                // ReSharper disable once MethodHasAsyncOverload
                backend1.InvalidateDependency( dependencyKey );

                Assert.True(
                    await Task.WhenAll( itemRemovedEvents.Select( e => e.Task ) ).WithTimeout( _timeoutTimeSpan ),
                    "ItemsRemoved event not received." );

                for ( var i = 0; i < backends.Length; i++ )
                {
                    var backend2 = backends[i];

                    this.TestOutputHelper.WriteLine( $"Testing with slave backend #{i}: {backend2}" );

                    Assert.False( await backend2.ContainsItemAsync( itemKey ), $"Backend {i} still contains the item." );
                    Assert.False( await backend2.ContainsDependencyAsync( dependencyKey ), $"Backend {i} still contains the dependency." );
                }
            }
        }
        finally
        {
            await DisposeAndVerifyBackendsAsync( backends );
        }
    }

    [Fact]
    public void TestInvalidateDependencyIdenticalItems()
    {
        if ( !this.TestDependencies )
        {
            AssertEx.Inconclusive();

            return;
        }

        this.ConnectToRedisIfRequired();
        const string dependencyKey = "d", itemKey = "i";

        var backends = this.CreateBackends();

        try
        {
            var itemRemovedEvents = new ManualResetEventSlim[backends.Length];

            for ( var j = 0; j < backends.Length; j++ )
            {
                var index = j;
                backends[j].ItemRemoved += ( _, _ ) => itemRemovedEvents[index].Set();
            }

            for ( var j = 0; j < 1; j++ )
            {
                var backend1 = backends[j];
                this.TestOutputHelper.WriteLine( $"Testing with master backend #{j}: {backend1}" );

                for ( var i = 0; i < backends.Length; i++ )
                {
                    itemRemovedEvents[i] = new ManualResetEventSlim( false );
                    var backend2 = backends[i];
                    backend2.SetItem( itemKey, new CacheItem( "Hello, world.", ImmutableArray.Create( dependencyKey ) ) );
                }

                backend1.InvalidateDependency( dependencyKey );

                for ( var i = 0; i < backends.Length; i++ )
                {
                    Assert.True( itemRemovedEvents[i].Wait( _timeout ) );
                }

                for ( var i = 0; i < backends.Length; i++ )
                {
                    this.TestOutputHelper.WriteLine( "Waiting..." );
                    var backend2 = backends[i];

                    this.TestOutputHelper.WriteLine( $"Testing with slave backend #{i}: {backend2}" );

                    Assert.False( backend2.ContainsItem( itemKey ), $"Backend {i} still contains the item." );
                    Assert.False( backend2.ContainsDependency( dependencyKey ), $"Backend {i} still contains the dependency." );
                }
            }
        }
        finally
        {
            DisposeAndVerifyBackends( backends );
        }
    }

    [Fact( Timeout = _timeout )]
    public async Task TestRemoveSharedItemAsync()
    {
        this.ConnectToRedisIfRequired();
        const string dependencyKey = "d", itemKey = "i";

        var backends = await this.CreateBackendsAsync();

        try
        {
            var itemRemovedEvents = new TaskCompletionSource<bool>[backends.Length];

            for ( var j = 0; j < backends.Length; j++ )
            {
                var index = j;
                backends[j].ItemRemoved += ( _, _ ) => itemRemovedEvents[index].TrySetResult( true );
            }

            for ( var j = 0; j < backends.Length; j++ )
            {
                var backend1 = backends[j];
                this.TestOutputHelper.WriteLine( $"Testing with master backend #{j}: {backend1}" );

                for ( var i = 0; i < backends.Length; i++ )
                {
                    var backend2 = backends[i];

                    itemRemovedEvents[i] = new TaskCompletionSource<bool>();

                    CacheItem cacheItem;

                    if ( this.TestDependencies )
                    {
                        cacheItem = new CacheItem( "Hello, world.", ImmutableArray.Create( dependencyKey ) );
                    }
                    else
                    {
                        cacheItem = new CacheItem( "Hello, world." );
                    }

                    await backend2.SetItemAsync( itemKey, cacheItem );
                }

                await backend1.RemoveItemAsync( itemKey );

                Assert.True( await Task.WhenAll( itemRemovedEvents.Select( e => e.Task ) ).WithTimeout( _timeoutTimeSpan ) );

                for ( var i = 0; i < backends.Length; i++ )
                {
                    var backend2 = backends[i];

                    this.TestOutputHelper.WriteLine( $"Testing with slave backend #{i}: {backend2}" );

                    Assert.False( await backend2.ContainsItemAsync( itemKey ), $"Backend {i} still contains the item." );

                    if ( this.TestDependencies )
                    {
                        Assert.False( await backend2.ContainsDependencyAsync( dependencyKey ), $"Backend {i} still contains the dependency." );
                    }
                }
            }
        }
        finally
        {
            await DisposeAndVerifyBackendsAsync( backends );
        }
    }

    [Fact( Timeout = _timeout )]
    public async Task TestInvalidateDependencyDifferentItemsAsync()
    {
        if ( !this.TestDependencies )
        {
            AssertEx.Inconclusive();

            return;
        }

        this.ConnectToRedisIfRequired();
        const string dependencyKey = "d";

        var backends = await this.CreateBackendsAsync();

        try
        {
            var itemRemovedEvents = new TaskCompletionSource<bool>[backends.Length];

            for ( var j = 0; j < backends.Length; j++ )
            {
                var index = j;
                backends[j].ItemRemoved += ( _, _ ) => itemRemovedEvents[index].TrySetResult( true );
            }

            for ( var j = 0; j < backends.Length; j++ )
            {
                var backend1 = backends[j];

                this.TestOutputHelper.WriteLine( $"Testing with master backend #{j}: {backend1}" );

                for ( var i = 0; i < backends.Length; i++ )
                {
                    var backend2 = backends[i];
                    await backend2.SetItemAsync( $"i{i}", new CacheItem( "Hello, world.", ImmutableArray.Create( dependencyKey ) ) );
                    itemRemovedEvents[i] = new TaskCompletionSource<bool>();
                }

                await backend1.InvalidateDependencyAsync( dependencyKey );

                Assert.True( await Task.WhenAll( itemRemovedEvents.Select( e => e.Task ) ).WithTimeout( _timeoutTimeSpan ) );

                for ( var i = 0; i < backends.Length; i++ )
                {
                    var backend2 = backends[i];

                    this.TestOutputHelper.WriteLine( $"Testing with slave backend #{i}: {backend2}" );

                    Assert.False(
                        await backends[i].ContainsItemAsync( $"i{i}" ),
                        $"Backend {i} still contains the item but it was invalidated by backend {j}." );

                    Assert.False(
                        await backends[i].ContainsDependencyAsync( dependencyKey ),
                        $"Backend {i} still contains the dependency but it was invalidated by backend {j}." );
                }
            }
        }
        finally
        {
            await DisposeAndVerifyBackendsAsync( backends );
        }
    }

    [Fact( Timeout = _timeout )]
    public async Task TestRemoveDifferentItemsAsync()
    {
        this.ConnectToRedisIfRequired();
        const string dependencyKey = "d";

        var backends = await this.CreateBackendsAsync();

        try
        {
            var itemRemovedEvents = new TaskCompletionSource<bool>[backends.Length];

            for ( var j = 0; j < backends.Length; j++ )
            {
                var index = j;
                backends[j].ItemRemoved += ( _, _ ) => itemRemovedEvents[index].TrySetResult( true );
            }

            for ( var j = 0; j < backends.Length; j++ )
            {
                var backend1 = backends[j];

                this.TestOutputHelper.WriteLine( $"Testing with master backend #{j}: {backend1}" );

                for ( var i = 0; i < backends.Length; i++ )
                {
                    itemRemovedEvents[i] = new TaskCompletionSource<bool>();

                    var backend2 = backends[i];
                    CacheItem cacheItem;

                    if ( this.TestDependencies )
                    {
                        cacheItem = new CacheItem( "Hello, world.", ImmutableArray.Create( dependencyKey ) );
                    }
                    else
                    {
                        cacheItem = new CacheItem( "Hello, world." );
                    }

                    await backend2.SetItemAsync( $"i{i}", cacheItem );
                }

                for ( var i = 0; i < backends.Length; i++ )
                {
                    // We intentionally remove from a different backend than the one that added.
                    await backend1.RemoveItemAsync( $"i{i}" );
                }

                Assert.True( await Task.WhenAll( itemRemovedEvents.Select( e => e.Task ) ).WithTimeout( _timeoutTimeSpan ) );

                for ( var i = 0; i < backends.Length; i++ )
                {
                    this.TestOutputHelper.WriteLine( $"Testing with slave backend #{i}: {backends[i]}" );

                    // [Porting] Not fixing, cannot be certain of original intent.
                    // ReSharper disable once MethodHasAsyncOverload
                    Assert.False( backends[i].ContainsItem( $"i{i}" ), $"Backend {i} still contains the item." );

                    if ( this.TestDependencies )
                    {
                        // [Porting] Not fixing, cannot be certain of original intent.
                        // ReSharper disable once MethodHasAsyncOverload
                        Assert.False( backends[i].ContainsDependency( dependencyKey ), $"Backend {i} still contains the dependency." );
                    }
                }
            }
        }
        finally
        {
            await DisposeAndVerifyBackendsAsync( backends );
        }
    }
}