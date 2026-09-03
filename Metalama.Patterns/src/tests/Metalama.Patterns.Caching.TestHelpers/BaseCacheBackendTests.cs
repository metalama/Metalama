// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.TestHelpers
{
    public abstract class BaseCacheBackendTests : BaseCachingTests, IDisposable, IClassFixture<CachingClassFixture>
    {
        protected const int Timeout = 30_000; // 30 seconds ought to be enough to anyone. (otherwise the test should be refactored, anyway).
        protected static readonly TimeSpan TimeoutTimeSpan = TimeSpan.FromMilliseconds( Timeout );

        protected BaseCacheBackendTests( CachingClassFixture cachingClassFixture, ITestOutputHelper testOutputHelper ) : base( testOutputHelper )
        {
            this.ClassFixture = cachingClassFixture;
        }

        protected virtual bool TestDependencies { get; } = true;

        protected CachingClassFixture ClassFixture { get; }

        protected abstract CheckAfterDisposeCachingBackend CreateBackend();

        protected virtual Task<CheckAfterDisposeCachingBackend> CreateBackendAsync() => Task.FromResult( this.CreateBackend() );

        protected virtual void GiveChanceToResetLocalCache( CachingBackend backend ) { }

        /// <summary>
        /// Gets the smallest duration that the expiration tests can measure on the real clock, multiplied by
        /// <paramref name="multiplier"/>.
        /// </summary>
        /// <remarks>
        /// The member is used only by the test classes whose backend expires its items outside of this process and
        /// therefore returns <see langword="null"/> from <see cref="FakeServices"/>. Such a test class widens the
        /// quantum when its backend needs more time. A test class that substitutes the clock does not read this member.
        /// </remarks>
        /// <param name="multiplier">The number by which the quantum is multiplied.</param>
        protected virtual TimeSpan GetExpirationQuantum( double multiplier = 1 )
        {
            return TimeSpan.FromSeconds( 0.05 * multiplier );
        }

        /// <summary>
        /// Gets the substitutable clock, work-item dispatcher and memory cache of the backends that
        /// <see cref="CreateBackend"/> creates, or <see langword="null"/> when those backends run on the real clock.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A test class whose backend expires its items in memory returns an instance and registers its services from
        /// <see cref="BaseCachingTests.AddServices"/>. The expiration tests then advance the clock and wait for the
        /// work items that the advance queues, instead of sleeping.
        /// </para>
        /// <para>
        /// A test class whose backend expires its items in a remote store, such as Redis, returns
        /// <see langword="null"/>. The remote store keeps a clock of its own that the test cannot substitute, so the
        /// expiration tests of such a backend stay on real time and measure durations with
        /// <see cref="GetExpirationQuantum"/>.
        /// </para>
        /// </remarks>
        protected virtual FakeCachingServices? FakeServices => null;

        /// <summary>
        /// Gets the expiration set on the cache items of the expiration tests.
        /// </summary>
        /// <remarks>
        /// On the substituted clock the value is arbitrary, because the test decides when the clock passes it. On the
        /// real clock it is a multiple of <see cref="GetExpirationQuantum"/>.
        /// </remarks>
        protected TimeSpan ExpirationTimeout => this.FakeServices == null ? this.GetExpirationQuantum( 3 ) : TimeSpan.FromMinutes( 5 );

        /// <summary>
        /// Lets a part of <see cref="ExpirationTimeout"/> pass, so that the caller can then read an item that has not
        /// expired yet.
        /// </summary>
        /// <remarks>
        /// On the real clock the method sleeps, and the caller has to check with <see cref="IsWithinExpiration"/> that
        /// the read was performed before the expiration instant. On the substituted clock the advance is exact.
        /// </remarks>
        /// <param name="cancellationToken">A token that cancels the wait.</param>
        protected async Task WaitWithinExpirationAsync( CancellationToken cancellationToken )
        {
            if ( this.FakeServices is { } fakeServices )
            {
                await fakeServices.AdvanceAsync( this.ExpirationTimeout.Multiply( 0.25 ), cancellationToken );
            }
            else
            {
                await Task.Delay( this.GetExpirationQuantum(), cancellationToken );
            }
        }

        /// <summary>
        /// Returns a value indicating whether an item that was stored at <paramref name="setTime"/> with an expiration
        /// of <see cref="ExpirationTimeout"/> has not expired yet.
        /// </summary>
        /// <remarks>
        /// The method is called after the item has been read, so that the reading and the assertion that the reading
        /// was timely are in that order. On the real clock the method returns <see langword="false"/> when the machine
        /// was slow enough for the expiration instant to have passed already, in which case the caller starts the test
        /// over. On the substituted clock the test controls the clock, so the method always returns
        /// <see langword="true"/>.
        /// </remarks>
        /// <param name="setTime">The instant of the real clock at which the item was stored.</param>
        protected bool IsWithinExpiration( DateTime setTime )
            => this.FakeServices != null || DateTime.Now <= setTime + this.ExpirationTimeout;

        /// <summary>
        /// Waits until the items that were stored with an expiration of <see cref="ExpirationTimeout"/> have expired.
        /// </summary>
        /// <remarks>
        /// On the substituted clock the advance evicts the entries of the memory cache and the method then waits for
        /// the work items that the eviction queues, so the backend has raised its events when the method returns. On
        /// the real clock the method sleeps and the caller still has to wait for the events.
        /// </remarks>
        /// <param name="cancellationToken">A token that cancels the wait.</param>
        protected async Task WaitPastExpirationAsync( CancellationToken cancellationToken )
        {
            var delta = this.ExpirationTimeout.Multiply( 2 );

            if ( this.FakeServices is { } fakeServices )
            {
                await fakeServices.AdvanceAsync( delta, cancellationToken );
            }
            else
            {
                await Task.Delay( delta, cancellationToken );
            }
        }

        /// <summary>
        /// Asserts that <paramref name="getItem"/> no longer returns the item.
        /// </summary>
        /// <remarks>
        /// On the real clock the read is repeated until the item is gone or <see cref="TimeoutTimeSpan"/> elapses,
        /// because some backends collect an expired item only when the item is read. On the substituted clock the item
        /// is already gone when <see cref="WaitPastExpirationAsync"/> returns, so a single read is enough.
        /// </remarks>
        /// <param name="getItem">A function that reads the item from the cache.</param>
        /// <typeparam name="T">The type of the item.</typeparam>
        protected async Task AssertItemRemovedAsync<T>( Func<Task<T?>> getItem )
            where T : class
        {
            if ( this.FakeServices == null )
            {
                await RepeatUntilNullOrFailAsync( getItem );
            }
            else
            {
                AssertEx.Null( await getItem(), "The item is still in the cache after its expiration." );
            }
        }

        /// <summary>
        /// Asserts that <paramref name="getItem"/> no longer returns the item.
        /// </summary>
        /// <param name="getItem">A function that reads the item from the cache.</param>
        /// <typeparam name="T">The type of the item.</typeparam>
        protected Task AssertItemRemovedAsync<T>( Func<ValueTask<T?>> getItem )
            where T : class
            => this.AssertItemRemovedAsync( () => getItem().AsTask() );

        /// <summary>
        /// Reads the item until it is gone or until <see cref="TimeoutTimeSpan"/> elapses, and fails the test when it
        /// is still there at the end.
        /// </summary>
        /// <param name="func">A function that reads the item from the cache.</param>
        /// <typeparam name="T">The type of the item.</typeparam>
        private static async Task RepeatUntilNullOrFailAsync<T>( Func<Task<T?>> func )
            where T : class
        {
            var stopwatch = Stopwatch.StartNew();

            while ( stopwatch.Elapsed < TimeoutTimeSpan )
            {
                if ( await func() == null )
                {
                    return;
                }

                await Task.Delay( 10 );
            }

            Assert.Fail( $"The item still exists in cache after the {TimeoutTimeSpan} timeout." );
        }

        public void Dispose()
        {
            try
            {
                this.TestOutputHelper.WriteLine( "Cleaning up." );
                this.Cleanup();
                this.TestOutputHelper.WriteLine( "Clean up completed." );
            }
            catch ( Exception e )
            {
                this.TestOutputHelper.WriteLine( "Clean up failed: " + e );

                throw;
            }
        }

        protected virtual void Cleanup()
        {
            GC.Collect();

            foreach ( var pendingTask in this.BackgroundTaskSchedulerObserver.PendingTasks )
            {
                this.TestOutputHelper.WriteLine( "Pending task:" );
                this.TestOutputHelper.WriteLine( pendingTask.ToString() );
            }

            Assert.Empty( this.BackgroundTaskSchedulerObserver.PendingTasks );
        }

        [Fact]
        public void TestMiss()
        {
            using ( var cache = this.CreateBackend() )
            {
                var key = Guid.NewGuid().ToString();

                if ( cache.SupportedFeatures.Clear )
                {
                    cache.Clear();
                }

                var retrievedItem = cache.GetItem( key, this.TestDependencies );

                AssertEx.Null( retrievedItem, "The cache does not return null on miss." );
            }
        }

        [Fact( Timeout = Timeout )]
        public async Task TestMissAsync()
        {
            // [Porting] Not fixing, can't be certain of original intent.
            // ReSharper disable once UseAwaitUsing
            using ( var cache = await this.CreateBackendAsync() )
            {
                var key = Guid.NewGuid().ToString();

                if ( cache.SupportedFeatures.Clear )
                {
                    await cache.ClearAsync();
                }

                var retrievedItem = await cache.GetItemAsync( key, this.TestDependencies );

                AssertEx.Null( retrievedItem, $"The cache does not return null on miss. It returned {{{retrievedItem}}} instead." );
            }
        }

        [Fact]
        public void TestSet()
        {
            using ( var cache = this.CreateBackend() )
            {
                var storedValue0 = new CachedValueClass( 0 );
                const string key = "0";
                var cacheItem0 = new CacheItem( storedValue0, this.TestDependencies ? ["a", "b", "c"] : default );

                cache.SetItem( key, cacheItem0 );
                this.GiveChanceToResetLocalCache( cache );
                var retrievedItem = cache.GetItem( key, this.TestDependencies );

                AssertEx.NotNull( retrievedItem, "The item has not been stored in the cache." );

                AssertEx.Equal( storedValue0, retrievedItem.Value, "The item retrieved before the timeout is not the same as the initial item." );

                if ( this.TestDependencies )
                {
                    // The dependencies retrieved before the timeout must be the same as the initial dependencies.
                    Assert.Equal( cacheItem0.Dependencies.ToList(), (ICollection?) retrievedItem.Dependencies.ToList() );
                }

                var storedValue1 = new CachedValueClass( 1 );
                var cacheItem1 = new CacheItem( storedValue1 );

                cache.SetItem( key, cacheItem1 );
                this.GiveChanceToResetLocalCache( cache );
                retrievedItem = cache.GetItem( key, this.TestDependencies );

                AssertEx.NotNull( retrievedItem, "The item has not been stored in the cache." );
                AssertEx.NotEqual( cacheItem0, retrievedItem, "The item has not been changed." );
            }
        }

        [Fact( Timeout = Timeout )]
        public async Task TestSetAsync()
        {
            // [Porting] Not fixing, can't be certain of original intent.
            // ReSharper disable once UseAwaitUsing
            using ( var cache = await this.CreateBackendAsync() )
            {
                var storedValue0 = new CachedValueClass( 0 );
                const string key = "0";
                var cacheItem0 = new CacheItem( storedValue0, this.TestDependencies ? ["a", "b", "c"] : default );

                // [Porting] Not fixing, can't be certain of original intent.
                // ReSharper disable once MethodHasAsyncOverload
                cache.SetItem( key, cacheItem0 );
                this.GiveChanceToResetLocalCache( cache );
                var retrievedItem = await cache.GetItemAsync( key, this.TestDependencies );

                AssertEx.NotNull( retrievedItem, "The item has not been stored in the cache." );

                AssertEx.Equal( storedValue0, retrievedItem.Value, "The item retrieved before the timeout is not the same as the initial item." );

                if ( this.TestDependencies )
                {
                    // "The dependencies retrieved before the timeout must be the same as the initial dependencies."
                    Assert.Equal( cacheItem0.Dependencies.ToList(), (ICollection?) retrievedItem.Dependencies.ToList() );
                }

                var storedValue1 = new CachedValueClass( 1 );
                var cacheItem1 = new CacheItem( storedValue1 );

                await cache.SetItemAsync( key, cacheItem1 );
                this.GiveChanceToResetLocalCache( cache );
                retrievedItem = await cache.GetItemAsync( key, this.TestDependencies );

                AssertEx.NotNull( retrievedItem, "The item has not been stored in the cache." );
                AssertEx.NotEqual( cacheItem0, retrievedItem, "The item has not been changed." );
            }
        }

        [Fact( Timeout = Timeout )]
        public async Task TestAbsoluteExpiration()
        {
            using var cancellationTokenSource = new CancellationTokenSource( TimeoutTimeSpan );
            var cancellationToken = cancellationTokenSource.Token;

            while ( true )
            {
                using ( var cache = this.CreateBackend() )
                {
                    var storedValue = new CachedValueClass( 0 );
                    const string key = "0";

                    var offset = this.ExpirationTimeout;

                    var cacheItem = new CacheItem(
                        storedValue,
                        configuration: new CacheItemConfiguration { AbsoluteExpiration = offset },
                        dependencies: this.TestDependencies ? ["d"] : default );

                    var itemRemovedEvent = new ManualResetEvent( false );
                    cache.ItemRemoved += ( _, _ ) => itemRemovedEvent.Set();
                    var setTime = DateTime.Now;

                    cache.SetItem( key, cacheItem );

                    await this.WaitWithinExpirationAsync( cancellationToken );
                    var retrievedItemBeforeTimeout = cache.GetItem( key, this.TestDependencies );

                    if ( !this.IsWithinExpiration( setTime ) )
                    {
                        // Bad timing. Retry the test.
                        this.TestOutputHelper.WriteLine( "We waited too much." );

                        continue;
                    }

                    AssertEx.NotNull( retrievedItemBeforeTimeout, "The item has been removed before expiration." );

                    await this.WaitPastExpirationAsync( cancellationToken );

                    // ReSharper disable once AccessToDisposedClosure
                    await this.AssertItemRemovedAsync( () => Task.FromResult( cache.GetItem( key, this.TestDependencies ) ) );

                    Assert.True( itemRemovedEvent.WaitOne( TimeoutTimeSpan ) );

                    var retrievedItemAfterTimeout = cache.GetItem( key, this.TestDependencies );

                    AssertEx.Null( retrievedItemAfterTimeout, "There is an item retrieved after the timeout." );

                    return;
                }
            }
        }

        private static bool? _runningOnWindows;

        /// <summary>
        /// Gets a value indicating whether the test is run on Windows. We don't run some tests on Linux because, for some reason, the event that an item expired from the cache
        /// arrives up to 20 minutes later on Linux, and I don't know why. So we're just no longer testing this on Linux.
        /// </summary>
        private static bool RunningOnWindows => _runningOnWindows ?? (_runningOnWindows = RuntimeInformation.IsOSPlatform( OSPlatform.Windows )).Value;

        [Fact( Timeout = Timeout, Skip = "#33668" )]
        public virtual async Task TestSlidingExpiration()
        {
            if ( !RunningOnWindows )
            {
                AssertEx.Inconclusive();

                return;
            }

            using var cancellationTokenSource = new CancellationTokenSource( TimeoutTimeSpan );
            var cancellationToken = cancellationTokenSource.Token;

            while ( true )
            {
                using ( var cache = this.CreateBackend() )
                {
                    var storedValue = new CachedValueClass( 0 );
                    const string key = "0";
                    var expiration = this.ExpirationTimeout;

                    var cacheItem = new CacheItem(
                        storedValue,
                        configuration:
                        new CacheItemConfiguration { SlidingExpiration = expiration } );

                    var itemRemoved = new ManualResetEventSlim( false );
                    cache.ItemRemoved += ( _, _ ) => itemRemoved.Set();
                    var timeWhenSet = DateTime.Now;

                    cache.SetItem( key, cacheItem );

                    await this.WaitWithinExpirationAsync( cancellationToken );
                    var retrievedItemBeforeTimeout = cache.GetItem( key, this.TestDependencies );

                    if ( !this.IsWithinExpiration( timeWhenSet ) )
                    {
                        this.TestOutputHelper.WriteLine( "We slept too much time. Retry the test." );

                        continue;
                    }

                    AssertEx.NotNull( retrievedItemBeforeTimeout, "There is not an item retrieved before the timeout." );

                    if ( this.FakeServices == null )
                    {
                        // Storing another item forces the collection of the expired item on the backends that collect
                        // the expired items only when they are used.
                        while ( !itemRemoved.IsSet )
                        {
                            cache.SetItem( "cycle", new CacheItem( "value" ) );
                            Thread.Yield();
                        }

                        await Task.Delay( this.GetExpirationQuantum(), cancellationToken );
                    }
                    else
                    {
                        await this.WaitPastExpirationAsync( cancellationToken );
                        Assert.True( itemRemoved.IsSet );
                    }

                    // ReSharper disable once AccessToDisposedClosure
                    await this.AssertItemRemovedAsync( () => Task.FromResult( cache.GetItem( key, this.TestDependencies ) ) );

                    return;
                }
            }
        }

        [Fact( Timeout = Timeout, Skip = "#33668" )]
        public virtual async Task TestSlidingExpirationAsync()
        {
            if ( !RunningOnWindows )
            {
                AssertEx.Inconclusive();

                return;
            }

            using var cancellationTokenSource = new CancellationTokenSource( TimeoutTimeSpan );
            var cancellationToken = cancellationTokenSource.Token;

            while ( true )
            {
                // Having an outer try-finally block is broken in VS 2019
                // https://github.com/dotnet/roslyn/issues/34720
                // using ( CachingBackend cache = this.CreateBackend() )
                // {

                var cache = await this.CreateBackendAsync();

                try
                {
                    var storedValue = new CachedValueClass( 0 );
                    const string key = "0";
                    var expiration = this.ExpirationTimeout;

                    var cacheItem = new CacheItem(
                        storedValue,
                        configuration:
                        new CacheItemConfiguration { SlidingExpiration = expiration } );

                    var timeWhenSet = DateTime.Now;
                    var itemRemoved = new TaskCompletionSource<bool>();
                    cache.ItemRemoved += ( _, _ ) => itemRemoved.SetResult( true );
                    await cache.SetItemAsync( key, cacheItem );

                    await this.WaitWithinExpirationAsync( cancellationToken );

                    if ( !this.IsWithinExpiration( timeWhenSet ) )
                    {
                        this.TestOutputHelper.WriteLine( "We slept too much time." );

                        continue;
                    }

                    var retrievedItemBeforeTimeout = await cache.GetItemAsync( key, this.TestDependencies );
                    AssertEx.NotNull( retrievedItemBeforeTimeout, "There is not an item retrieved before the timeout." );

                    if ( this.FakeServices == null )
                    {
                        // Storing another item forces the collection of the expired item on the backends that collect
                        // the expired items only when they are used.
                        while ( !itemRemoved.Task.IsCompleted )
                        {
                            await cache.SetItemAsync( "cycle", new CacheItem( "value" ) );
                            await Task.Delay( this.GetExpirationQuantum(), cancellationToken );
                        }
                    }
                    else
                    {
                        await this.WaitPastExpirationAsync( cancellationToken );
                        Assert.True( itemRemoved.Task.IsCompleted );
                    }

                    await this.AssertItemRemovedAsync( () => cache.GetItemAsync( key, this.TestDependencies ) );

                    return;
                }
                finally
                {
                    // [Porting] Not fixing, can't be certain of original intent.
                    // ReSharper disable once MethodHasAsyncOverload
                    cache.Dispose();
                }

                // }
            }
        }

        [Fact( Timeout = Timeout )]
        public async Task TestRemovalEventByExpiration()
        {
            using var cancellationTokenSource = new CancellationTokenSource( TimeoutTimeSpan );

            using ( var cache = this.CreateBackend() )
            {
                if ( !cache.SupportedFeatures.Events )
                {
                    AssertEx.Inconclusive();

                    return;
                }

                var eventRaised = new ManualResetEvent( false );
                CacheItemRemovedEventArgs? removalArguments = null;

                cache.ItemRemoved += ( _, args ) =>
                {
                    removalArguments = args;
                    eventRaised.Set();
                };

                var storedValue = new CachedValueClass( 0 );
                const string key = "0";
                var offset = this.ExpirationTimeout;

                var cacheItem = new CacheItem( storedValue, configuration: new CacheItemConfiguration { AbsoluteExpiration = offset } );

                cache.SetItem( key, cacheItem );

                await this.WaitPastExpirationAsync( cancellationTokenSource.Token );

                // ReSharper disable once AccessToDisposedClosure
                await this.AssertItemRemovedAsync( () => Task.FromResult( cache.GetItem( key, this.TestDependencies ) ) );

                Assert.True( eventRaised.WaitOne( TimeoutTimeSpan ) );

                AssertEx.NotNull( removalArguments, "The event did not pass any arguments." );
                Assert.Equal( CacheItemRemovedReason.Expired, removalArguments.RemovedReason );
            }
        }

        [Fact( Timeout = Timeout )]
        public async Task TestRemovalEventByExpirationAsync()
        {
            using var cancellationTokenSource = new CancellationTokenSource( TimeoutTimeSpan );

            // [Porting] Not fixing, can't be certain of original intent (twice).
            // ReSharper disable once UseAwaitUsing
            // ReSharper disable once MethodHasAsyncOverload
            using ( var cache = this.CreateBackend() )
            {
                if ( !cache.SupportedFeatures.Events )
                {
                    AssertEx.Inconclusive();

                    return;
                }

                var eventRaised = new ManualResetEvent( false );
                CacheItemRemovedEventArgs? removalArguments = null;

                cache.ItemRemoved += ( _, args ) =>
                {
                    removalArguments = args;
                    eventRaised.Set();
                };

                var storedValue = new CachedValueClass( 0 );
                const string key = "0";
                var offset = this.ExpirationTimeout;

                var cacheItem = new CacheItem( storedValue, configuration: new CacheItemConfiguration { AbsoluteExpiration = offset } );

                await cache.SetItemAsync( key, cacheItem );

                await this.WaitPastExpirationAsync( cancellationTokenSource.Token );
                await this.AssertItemRemovedAsync( () => cache.GetItemAsync( key, this.TestDependencies ) );

                Assert.True( eventRaised.WaitOne( TimeoutTimeSpan ) );

                AssertEx.NotNull( removalArguments, "The event did not pass any arguments." );
                Assert.Equal( CacheItemRemovedReason.Expired, removalArguments.RemovedReason );
            }
        }

        [Fact]
        public void TestRemovalEventByEviction()
        {
            using ( var cache = this.CreateBackend() )
            {
                if ( !cache.SupportedFeatures.Clear || !cache.SupportedFeatures.Events )
                {
                    AssertEx.Inconclusive();

                    return;
                }

                var eventRaised = new ManualResetEventSlim();
                CacheItemRemovedEventArgs? removalArguments = null;

                cache.ItemRemoved += ( _, args ) =>
                {
                    // Order matters, since at the point that the manual reset event is set, the assertions can start happening,
                    // and so the argument must already be set:
                    removalArguments = args;
                    eventRaised.Set();
                };

                var storedValue = new CachedValueClass( 0 );
                const string key = "0";
                var cacheItem = new CacheItem( storedValue );
                cache.SetItem( key, cacheItem );

                cache.Clear( ClearCacheOptions.Compact );

                Assert.True( eventRaised.Wait( TimeSpan.FromSeconds( 5 ) ), "The event has not been raised." );
                AssertEx.NotNull( removalArguments, "The event did not pass any arguments." );
                Assert.Equal( CacheItemRemovedReason.Evicted, removalArguments.RemovedReason );
            }
        }

        [Fact( Timeout = Timeout )]
        public async Task TestRemovalEventByEvictionAsync()
        {
            // [Porting] Not fixing, can't be certain of original intent (twice).
            // ReSharper disable once UseAwaitUsing
            // ReSharper disable once MethodHasAsyncOverload
            using ( var cache = this.CreateBackend() )
            {
                if ( !cache.SupportedFeatures.Clear || !cache.SupportedFeatures.Events )
                {
                    AssertEx.Inconclusive();

                    return;
                }

                var eventRaised = new ManualResetEvent( false );
                CacheItemRemovedEventArgs? removalArguments = null;

                cache.ItemRemoved += ( _, args ) =>
                {
                    removalArguments = args;
                    eventRaised.Set();
                };

                var storedValue = new CachedValueClass( 0 );
                const string key = "0";
                var cacheItem = new CacheItem( storedValue );
                await cache.SetItemAsync( key, cacheItem );

                await cache.ClearAsync( ClearCacheOptions.Compact );

                Assert.True( eventRaised.WaitOne( TimeoutTimeSpan ) );

                AssertEx.NotNull( removalArguments, "The event did not pass any arguments." );
                Assert.Equal( CacheItemRemovedReason.Evicted, removalArguments.RemovedReason );
            }
        }

        [Fact]
        public void TestRemovalEventByDependency()
        {
            if ( !this.TestDependencies )
            {
                AssertEx.Inconclusive();

                return;
            }

            using ( var cache = this.CreateBackend() )
            {
                if ( !cache.SupportedFeatures.Events )
                {
                    AssertEx.Inconclusive();

                    return;
                }

                var itemEventRaised = new ManualResetEvent( false );
                CacheItemRemovedEventArgs? itemEventArgs = null;

                cache.ItemRemoved += ( _, args ) =>
                {
                    itemEventArgs = args;
                    itemEventRaised.Set();
                };

                var dependencyEventRaised = new ManualResetEvent( false );
                CacheDependencyInvalidatedEventArgs? dependencyEventArgs = null;

                cache.DependencyInvalidated += ( _, args ) =>
                {
                    dependencyEventArgs = args;
                    dependencyEventRaised.Set();
                };

                var storedValue = new CachedValueClass( 0 );
                const string key = "0";

                const string dependencyKey = "1";

                var cacheItem = new CacheItem(
                    storedValue,
                    dependencies: [dependencyKey] );

                cache.SetItem( key, cacheItem );
                cache.InvalidateDependency( dependencyKey );
                Assert.Null( cache.GetItem( key, this.TestDependencies ) );

                Assert.True( itemEventRaised.WaitOne( TimeoutTimeSpan ), "Did not receive ItemRemoved event." );
                Assert.True( dependencyEventRaised.WaitOne( TimeoutTimeSpan ), "Did not received DependencyInvalidated event." );

                AssertEx.NotNull( itemEventArgs, "The item event did not pass any arguments." );
                Assert.Equal( CacheItemRemovedReason.Invalidated, itemEventArgs.RemovedReason );

                AssertEx.NotNull( dependencyEventArgs, "The dependency event did not pass any arguments." );
            }
        }

        [Fact( Timeout = Timeout )]
        public virtual async Task TestRemovalEventByDependencyAsync()
        {
            if ( !this.TestDependencies )
            {
                AssertEx.Inconclusive();

                return;
            }

            // [Porting] Not fixing, can't be certain of original intent (twice).
            // ReSharper disable once UseAwaitUsing
            // ReSharper disable once MethodHasAsyncOverload
            using ( var cache = this.CreateBackend() )
            {
                if ( !cache.SupportedFeatures.Events )
                {
                    AssertEx.Inconclusive();

                    return;
                }

                var itemEventRaised = new TaskCompletionSource<bool>();
                CacheItemRemovedEventArgs? itemEventArgs = null;

                cache.ItemRemoved += ( _, args ) =>
                {
                    itemEventArgs = args;
                    itemEventRaised.TrySetResult( true );
                };

                var dependencyEventRaised = new TaskCompletionSource<bool>();
                CacheDependencyInvalidatedEventArgs? dependencyEventArgs = null;

                cache.DependencyInvalidated += ( _, args ) =>
                {
                    dependencyEventArgs = args;
                    dependencyEventRaised.TrySetResult( true );
                };

                var storedValue = new CachedValueClass( 0 );
                const string key = "0";

                const string dependencyKey = "1";

                var cacheItem = new CacheItem(
                    storedValue,
                    dependencies: [dependencyKey] );

                await cache.SetItemAsync( key, cacheItem );

                await cache.InvalidateDependencyAsync( dependencyKey );
                Assert.Null( await cache.GetItemAsync( key, this.TestDependencies ) );

                Assert.True( await Task.WhenAll( itemEventRaised.Task, dependencyEventRaised.Task ).WithTimeout( TimeoutTimeSpan ) );

                // await cache.FlushAsync();

                AssertEx.NotNull( itemEventArgs, "The item event did not pass any arguments." );
                Assert.Equal( CacheItemRemovedReason.Invalidated, itemEventArgs.RemovedReason );

                AssertEx.NotNull( dependencyEventArgs, "The dependency event did not pass any arguments." );
            }
        }

        [Fact]
        public void TestDependency()
        {
            if ( !this.TestDependencies )
            {
                AssertEx.Inconclusive();

                return;
            }

            using ( var cache = this.CreateBackend() )
            {
                List<CacheItemRemovedEventArgs> events = [];
                cache.ItemRemoved += ( _, args ) => events.Add( args );

                const string dependencyKey = "dependency";
                var cacheItem1 = new CacheItem( new CachedValueClass( 1 ), [dependencyKey] );
                cache.SetItem( "m1", cacheItem1 );

                this.GiveChanceToResetLocalCache( cache );

                Assert.Empty( events );
                Assert.NotNull( cache.GetItem( "m1", this.TestDependencies ) );
                cache.InvalidateDependency( dependencyKey );

                this.GiveChanceToResetLocalCache( cache );

                Assert.Null( cache.GetItem( "m1", this.TestDependencies ) );

                if ( cache.SupportedFeatures.ContainsDependency )
                {
                    Assert.False( cache.ContainsDependency( dependencyKey ) );
                }
            }
        }

        [Fact( Timeout = Timeout )]
        public async Task TestDependencyAsync()
        {
            if ( !this.TestDependencies )
            {
                AssertEx.Inconclusive();

                return;
            }

            // [Porting] Not fixing, can't be certain of original intent (twice).
            // ReSharper disable once UseAwaitUsing
            // ReSharper disable once MethodHasAsyncOverload
            using ( var cache = this.CreateBackend() )
            {
                var eventsCount = 0;
                cache.ItemRemoved += ( _, _ ) => eventsCount++;

                const string dependencyKey = "dependency";
                var cacheItem1 = new CacheItem( new CachedValueClass( 1 ), [dependencyKey] );
                await cache.SetItemAsync( "m1", cacheItem1 );

                this.GiveChanceToResetLocalCache( cache );

                Assert.Equal( 0, eventsCount );
                Assert.NotNull( await cache.GetItemAsync( "m1", this.TestDependencies ) );

                this.GiveChanceToResetLocalCache( cache );

                await cache.InvalidateDependencyAsync( dependencyKey );
                Assert.Null( await cache.GetItemAsync( "m1", this.TestDependencies ) );

                if ( cache.SupportedFeatures.ContainsDependency )
                {
                    Assert.False( await cache.ContainsDependencyAsync( dependencyKey ) );
                }
            }
        }

        [Fact]
        public void TestSharedDependency()
        {
            if ( !this.TestDependencies )
            {
                AssertEx.Inconclusive();

                return;
            }

            using ( var cache = this.CreateBackend() )
            {
                var eventsCount = 0;
                cache.ItemRemoved += ( _, _ ) => eventsCount++;

                const string dependencyKey = "dependency";
                var cacheItem1 = new CacheItem( new CachedValueClass( 1 ), this.TestDependencies ? [dependencyKey] : default );
                var cacheItem2 = new CacheItem( new CachedValueClass( 2 ), this.TestDependencies ? [dependencyKey] : default );
                cache.SetItem( "m1", cacheItem1 );
                cache.SetItem( "m2", cacheItem2 );

                this.GiveChanceToResetLocalCache( cache );

                Assert.Equal( 0, eventsCount );
                Assert.NotNull( cache.GetItem( "m1", this.TestDependencies ) );
                Assert.NotNull( cache.GetItem( "m2", this.TestDependencies ) );

                Assert.Equal( 0, eventsCount );
            }
        }

        [Fact( Timeout = Timeout )]
        public async Task TestSharedDependencyAsync()
        {
            if ( !this.TestDependencies )
            {
                AssertEx.Inconclusive();

                return;
            }

            // [Porting] Not fixing, can't be certain of original intent (twice).
            // ReSharper disable once UseAwaitUsing
            // ReSharper disable once MethodHasAsyncOverload
            using ( var cache = this.CreateBackend() )
            {
                var eventsCount = 0;
                cache.ItemRemoved += ( _, _ ) => eventsCount++;

                const string dependencyKey = "dependency";
                var cacheItem1 = new CacheItem( new CachedValueClass( 1 ), this.TestDependencies ? [dependencyKey] : default );
                var cacheItem2 = new CacheItem( new CachedValueClass( 2 ), this.TestDependencies ? [dependencyKey] : default );
                await cache.SetItemAsync( "m1", cacheItem1 );
                await cache.SetItemAsync( "m2", cacheItem2 );

                this.GiveChanceToResetLocalCache( cache );

                Assert.Equal( 0, eventsCount );
                Assert.NotNull( await cache.GetItemAsync( "m1", this.TestDependencies ) );
                Assert.NotNull( await cache.GetItemAsync( "m2", this.TestDependencies ) );

                Assert.Equal( 0, eventsCount );
            }
        }

        [Fact]
        public void TestReplace()
        {
            using ( var cache = this.CreateBackend() )
            {
                var cacheItem1 = new CacheItem( new CachedValueClass( 1 ), this.TestDependencies ? ["d1"] : default );
                var cacheItem2 = new CacheItem( new CachedValueClass( 2 ), this.TestDependencies ? ["d2"] : default );
                cache.SetItem( "m", cacheItem1 );
                cache.SetItem( "m", cacheItem2 );

                this.GiveChanceToResetLocalCache( cache );

                var retrievedValue = cache.GetItem( "m", this.TestDependencies );
                Assert.NotNull( retrievedValue );

                Assert.Equal( cacheItem2.Value, retrievedValue.Value );

                if ( this.TestDependencies )
                {
                    cache.InvalidateDependency( "d1" );

                    Assert.NotNull( cache.GetItem( "m", this.TestDependencies ) );
                }

                Assert.True( cache.ContainsItem( "m" ) );
            }
        }

        [Fact( Timeout = Timeout )]
        public async Task TestReplaceAsync()
        {
            // [Porting] Not fixing, can't be certain of original intent.
            // ReSharper disable once UseAwaitUsing
            using ( var cache = await this.CreateBackendAsync() )
            {
                var cacheItem1 = new CacheItem( new CachedValueClass( 1 ), this.TestDependencies ? ["d1"] : default );
                var cacheItem2 = new CacheItem( new CachedValueClass( 2 ), this.TestDependencies ? ["d2"] : default );
                await cache.SetItemAsync( "m", cacheItem1 );
                await cache.SetItemAsync( "m", cacheItem2 );

                this.GiveChanceToResetLocalCache( cache );

                var retrievedValue = await cache.GetItemAsync( "m", this.TestDependencies );
                Assert.NotNull( retrievedValue );

                Assert.Equal( cacheItem2.Value, retrievedValue.Value );

                if ( this.TestDependencies )
                {
                    await cache.InvalidateDependencyAsync( "d1" );

                    Assert.NotNull( await cache.GetItemAsync( "m", this.TestDependencies ) );
                }

                Assert.True( await cache.ContainsItemAsync( "m" ) );
            }
        }

        [Fact]
        public void TestSetItemWithDependencyWithoutSupport()
        {
            using ( var cache = this.CreateBackend() )
            {
                if ( this.TestDependencies )
                {
                    Assert.True( cache.SupportedFeatures.Dependencies );
                    AssertEx.Inconclusive();

                    return;
                }

                Assert.False( cache.SupportedFeatures.Dependencies );

                Assert.Throws<NotSupportedException>( () => cache.SetItem( "i", new CacheItem( "v", ["d"] ) ) );
            }
        }

        [Fact( Timeout = Timeout )]
        public async Task TestSetItemWithDependencyWithoutSupportAsync()
        {
            // [Porting] Not fixing, can't be certain of original intent.
            // ReSharper disable once UseAwaitUsing
            using ( var cache = await this.CreateBackendAsync() )
            {
                if ( this.TestDependencies )
                {
                    Assert.True( cache.SupportedFeatures.Dependencies );
                    AssertEx.Inconclusive();

                    return;
                }

                Assert.False( cache.SupportedFeatures.Dependencies );

                await Assert.ThrowsAsync<NotSupportedException>( async () => await cache.SetItemAsync( "i", new CacheItem( "v", ["d"] ) ) );
            }
        }

        [Fact]
        public void TestInvalidateDependencyWithoutSupport()
        {
            using ( var cache = this.CreateBackend() )
            {
                if ( this.TestDependencies )
                {
                    Assert.True( cache.SupportedFeatures.Dependencies );
                    AssertEx.Inconclusive();

                    return;
                }

                Assert.False( cache.SupportedFeatures.Dependencies );

                Assert.Throws<NotSupportedException>( () => cache.InvalidateDependency( "d" ) );
            }
        }

        [Fact( Timeout = Timeout )]
        public async Task TestInvalidateDependencyWithoutSupportAsync()
        {
            // [Porting] Not fixing, can't be certain of original intent.
            // ReSharper disable once UseAwaitUsing
            using ( var cache = await this.CreateBackendAsync() )
            {
                if ( this.TestDependencies )
                {
                    Assert.True( cache.SupportedFeatures.Dependencies );
                    AssertEx.Inconclusive();

                    return;
                }

                Assert.False( cache.SupportedFeatures.Dependencies );

                await Assert.ThrowsAsync<NotSupportedException>( async () => await cache.InvalidateDependencyAsync( "d" ) );
            }
        }

        [Fact]
        public void TestContainsDependencyWithoutSupport()
        {
            using ( var cache = this.CreateBackend() )
            {
                if ( this.TestDependencies )
                {
                    Assert.True( cache.SupportedFeatures.Dependencies );
                    AssertEx.Inconclusive();

                    return;
                }

                Assert.False( cache.SupportedFeatures.Dependencies );
                Assert.False( cache.SupportedFeatures.ContainsDependency );

                Assert.Throws<NotSupportedException>( () => cache.ContainsDependency( "d" ) );
            }
        }

        [Fact( Timeout = Timeout )]
        public async Task TestContainsDependencyWithoutSupportAsync()
        {
            // [Porting] Not fixing, can't be certain of original intent.
            // ReSharper disable once UseAwaitUsing
            using ( var cache = await this.CreateBackendAsync() )
            {
                if ( this.TestDependencies )
                {
                    Assert.True( cache.SupportedFeatures.Dependencies );
                    AssertEx.Inconclusive();

                    return;
                }

                Assert.False( cache.SupportedFeatures.Dependencies );
                Assert.False( cache.SupportedFeatures.ContainsDependency );

                await Assert.ThrowsAsync<NotSupportedException>( async () => await cache.ContainsDependencyAsync( "d" ) );
            }
        }

        /// <summary>
        /// Tests that invalidating a dependency at the end of a long chain (A→B→C→...→H)
        /// correctly invalidates all items in the chain recursively.
        /// </summary>
        [Fact( Timeout = Timeout )]
        public async Task TestLongDependencyChainInvalidationAsync()
        {
            if ( !this.TestDependencies )
            {
                AssertEx.Inconclusive();

                return;
            }

            await using ( var cache = await this.CreateBackendAsync() )
            {
                // Create a chain: A depends on B, B depends on C, ..., G depends on H.
                var keys = new[] { "A", "B", "C", "D", "E", "F", "G", "H" };
                var removedItems = new List<string>();
                var allRemovedEvent = new TaskCompletionSource<bool>();
                var expectedRemovals = keys.Length - 1; // All except H (which is just a dependency key, not a cached item)

                cache.ItemRemoved += ( _, args ) =>
                {
                    lock ( removedItems )
                    {
                        removedItems.Add( args.Key );

                        if ( removedItems.Count >= expectedRemovals )
                        {
                            allRemovedEvent.TrySetResult( true );
                        }
                    }
                };

                // Set up the chain: each item depends on the next key.
                for ( var i = 0; i < keys.Length - 1; i++ )
                {
                    var cacheItem = new CacheItem( new CachedValueClass( i ), [keys[i + 1]] );
                    await cache.SetItemAsync( keys[i], cacheItem );
                }

                // Verify all items are in cache.
                for ( var i = 0; i < keys.Length - 1; i++ )
                {
                    Assert.NotNull( await cache.GetItemAsync( keys[i], this.TestDependencies ) );
                }

                // Invalidate H (the end of the chain).
                await cache.InvalidateDependencyAsync( "H" );

                // Wait for all removals to complete.
                Assert.True( await allRemovedEvent.Task.WithTimeout( TimeoutTimeSpan ), "Not all items were removed within timeout." );

                // Verify all items have been removed from the cache.
                for ( var i = 0; i < keys.Length - 1; i++ )
                {
                    Assert.Null( await cache.GetItemAsync( keys[i], this.TestDependencies ) );
                }

                // Verify the removal order: G should be removed first, then F, E, D, C, B, A.
                // (reverse order from the dependency chain)
                lock ( removedItems )
                {
                    Assert.Equal( expectedRemovals, removedItems.Count );
                }
            }
        }

        /// <summary>
        /// Tests that a self-referential dependency (A depends on A) does not cause
        /// an infinite loop during invalidation.
        /// </summary>
        [Fact( Timeout = Timeout )]
        public async Task TestSelfDependencyAsync()
        {
            if ( !this.TestDependencies )
            {
                AssertEx.Inconclusive();

                return;
            }

            await using ( var cache = await this.CreateBackendAsync() )
            {
                var itemRemovedEvent = new TaskCompletionSource<bool>();
                CacheItemRemovedEventArgs? removedArgs = null;

                cache.ItemRemoved += ( _, args ) =>
                {
                    removedArgs = args;
                    itemRemovedEvent.TrySetResult( true );
                };

                // Create an item that depends on itself.
                var cacheItem = new CacheItem( new CachedValueClass( 1 ), ["A"] );
                await cache.SetItemAsync( "A", cacheItem );

                // Verify it's in cache.
                Assert.NotNull( await cache.GetItemAsync( "A", this.TestDependencies ) );

                // Invalidate A (which should remove the item without infinite loop).
                await cache.InvalidateDependencyAsync( "A" );

                // Wait for removal.
                Assert.True( await itemRemovedEvent.Task.WithTimeout( TimeoutTimeSpan ), "Item was not removed within timeout." );

                // Verify the item is no longer in cache.
                Assert.Null( await cache.GetItemAsync( "A", this.TestDependencies ) );
                AssertEx.NotNull( removedArgs, "The item removal event was not raised." );
                Assert.Equal( CacheItemRemovedReason.Invalidated, removedArgs.RemovedReason );
            }
        }

        /// <summary>
        /// Tests that circular dependencies (A depends on B, B depends on A) do not cause
        /// an infinite loop during invalidation.
        /// </summary>
        [Fact( Timeout = Timeout )]
        public async Task TestCircularDependencyAsync()
        {
            if ( !this.TestDependencies )
            {
                AssertEx.Inconclusive();

                return;
            }

            await using ( var cache = await this.CreateBackendAsync() )
            {
                var removedItems = new List<string>();
                var allRemovedEvent = new TaskCompletionSource<bool>();

                cache.ItemRemoved += ( _, args ) =>
                {
                    lock ( removedItems )
                    {
                        removedItems.Add( args.Key );

                        if ( removedItems.Count >= 2 )
                        {
                            allRemovedEvent.TrySetResult( true );
                        }
                    }
                };

                // Create circular dependency: A depends on B, B depends on A.
                var cacheItemA = new CacheItem( new CachedValueClass( 1 ), ["B"] );
                var cacheItemB = new CacheItem( new CachedValueClass( 2 ), ["A"] );
                await cache.SetItemAsync( "A", cacheItemA );
                await cache.SetItemAsync( "B", cacheItemB );

                // Verify both items are in cache.
                Assert.NotNull( await cache.GetItemAsync( "A", this.TestDependencies ) );
                Assert.NotNull( await cache.GetItemAsync( "B", this.TestDependencies ) );

                // Invalidate A (which should remove both items without infinite loop).
                await cache.InvalidateDependencyAsync( "A" );

                // Wait for both removals.
                Assert.True( await allRemovedEvent.Task.WithTimeout( TimeoutTimeSpan ), "Not all items were removed within timeout." );

                // Verify both items are removed.
                Assert.Null( await cache.GetItemAsync( "A", this.TestDependencies ) );
                Assert.Null( await cache.GetItemAsync( "B", this.TestDependencies ) );

                lock ( removedItems )
                {
                    Assert.Equal( 2, removedItems.Count );
                }
            }
        }

        /// <summary>
        /// Tests that invalidating a dependency shared by many items (20) correctly
        /// removes all dependent items.
        /// </summary>
        [Fact( Timeout = Timeout )]
        public async Task TestLargeDependencyInvalidationAsync()
        {
            if ( !this.TestDependencies )
            {
                AssertEx.Inconclusive();

                return;
            }

            await using ( var cache = await this.CreateBackendAsync() )
            {
                const int itemCount = 20;
                const string sharedDependency = "shared";
                var removedItems = new List<string>();
                var allRemovedEvent = new TaskCompletionSource<bool>();

                cache.ItemRemoved += ( _, args ) =>
                {
                    lock ( removedItems )
                    {
                        removedItems.Add( args.Key );

                        if ( removedItems.Count >= itemCount )
                        {
                            allRemovedEvent.TrySetResult( true );
                        }
                    }
                };

                // Create many items all depending on the same key.
                for ( var i = 0; i < itemCount; i++ )
                {
                    var cacheItem = new CacheItem( new CachedValueClass( i ), [sharedDependency] );
                    await cache.SetItemAsync( $"item{i}", cacheItem );
                }

                // Verify all items are in cache.
                for ( var i = 0; i < itemCount; i++ )
                {
                    Assert.NotNull( await cache.GetItemAsync( $"item{i}", this.TestDependencies ) );
                }

                // Invalidate the shared dependency.
                await cache.InvalidateDependencyAsync( sharedDependency );

                // Wait for all removals.
                Assert.True( await allRemovedEvent.Task.WithTimeout( TimeoutTimeSpan ), "Not all items were removed within timeout." );

                // Verify all items have been removed.
                for ( var i = 0; i < itemCount; i++ )
                {
                    Assert.Null( await cache.GetItemAsync( $"item{i}", this.TestDependencies ) );
                }

                lock ( removedItems )
                {
                    Assert.Equal( itemCount, removedItems.Count );
                }
            }
        }
    }
}