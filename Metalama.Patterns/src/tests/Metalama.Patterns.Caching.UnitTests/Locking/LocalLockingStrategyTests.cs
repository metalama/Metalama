// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Locking;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Locking;

public sealed class LocalLockingStrategyTests
{
    private const string _testKey = "testKey";
    private const string _key1 = "key1";
    private const string _key2 = "key2";

    #region Basic Lock Acquisition Tests

    [Fact]
    public void GetLock_ReturnsLockHandle()
    {
        var strategy = new LocalLockingStrategy();

        using var lockHandle = strategy.GetLock( _testKey );

        Assert.NotNull( lockHandle );
    }

    [Fact]
    public void Acquire_Succeeds()
    {
        var strategy = new LocalLockingStrategy();

        using var lockHandle = strategy.GetLock( _testKey );
        var acquired = lockHandle.Acquire( TimeSpan.FromSeconds( 1 ), CancellationToken.None );
        lockHandle.Release();

        Assert.True( acquired );
    }

    [Fact]
    public async Task AcquireAsync_Succeeds()
    {
        var strategy = new LocalLockingStrategy();

        using var lockHandle = strategy.GetLock( _testKey );
        var acquired = await lockHandle.AcquireAsync( TimeSpan.FromSeconds( 1 ), CancellationToken.None );
        await lockHandle.ReleaseAsync();

        Assert.True( acquired );
    }

    [Fact]
    public async Task Acquire_SameKey_BlocksUntilReleased()
    {
        var strategy = new LocalLockingStrategy();
        var secondLockAcquired = new TaskCompletionSource<bool>();
        var secondTaskStartedWaiting = new TaskCompletionSource<bool>();
        var firstLockHeld = new TaskCompletionSource<bool>();
        var firstLockReleased = new TaskCompletionSource<bool>();
        using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );

        // First thread acquires lock
        var task1 = Task.Run(
            async () =>
            {
                using var lockHandle = strategy.GetLock( _testKey );
                lockHandle.Acquire( TimeSpan.FromSeconds( 10 ), CancellationToken.None );

                try
                {
                    firstLockHeld.SetResult( true );

                    // Wait until signaled to release
                    await firstLockReleased.Task.WaitWithTimeoutAsync();
                }
                finally
                {
                    lockHandle.Release();
                }
            } );

        // Wait for first lock to be acquired
        await firstLockHeld.Task.WaitWithTimeoutAsync();

        // Second thread tries to acquire same key - should block until released
        var task2 = Task.Run(
            () =>
            {
                using var lockHandle = strategy.GetLock( _testKey );
                secondTaskStartedWaiting.SetResult( true );

                // This should block until first lock is released, then succeed
                var acquired = lockHandle.Acquire( TimeSpan.FromSeconds( 10 ), CancellationToken.None );
                secondLockAcquired.SetResult( acquired );

                if ( acquired )
                {
                    lockHandle.Release();
                }
            } );

        // Wait for second task to start waiting
        await secondTaskStartedWaiting.Task.WaitWithTimeoutAsync();

        // Verify second lock has not been acquired yet (first lock still held)
        Assert.False( secondLockAcquired.Task.IsCompleted );

        // Release first lock
        firstLockReleased.SetResult( true );

        // Now wait for the second lock acquisition result
        var result = await secondLockAcquired.Task.WaitWithTimeoutAsync();

        // Second lock should have been acquired after first was released
        Assert.True( result );

        await Task.WhenAll( task1, task2 ).WaitWithTimeoutAsync();
    }

    #endregion

    #region Different Keys Tests

    [Fact]
    public async Task GetLock_DifferentKeys_IndependentLocks()
    {
        var strategy = new LocalLockingStrategy();
        var lock1Acquired = new TaskCompletionSource<bool>();
        var lock2Acquired = new TaskCompletionSource<bool>();
        var releaseLocks = new TaskCompletionSource<bool>();

        var task1 = Task.Run(
            async () =>
            {
                using var lockHandle = strategy.GetLock( _key1 );
                lockHandle.Acquire( TimeSpan.FromSeconds( 10 ), CancellationToken.None );

                try
                {
                    lock1Acquired.SetResult( true );
                    await releaseLocks.Task.WaitWithTimeoutAsync();
                }
                finally
                {
                    lockHandle.Release();
                }
            } );

        var task2 = Task.Run(
            async () =>
            {
                using var lockHandle = strategy.GetLock( _key2 );
                lockHandle.Acquire( TimeSpan.FromSeconds( 10 ), CancellationToken.None );

                try
                {
                    lock2Acquired.SetResult( true );
                    await releaseLocks.Task.WaitWithTimeoutAsync();
                }
                finally
                {
                    lockHandle.Release();
                }
            } );

        // Both locks should be acquired independently
        await lock1Acquired.Task.WaitWithTimeoutAsync();
        await lock2Acquired.Task.WaitWithTimeoutAsync();

        releaseLocks.SetResult( true );

        await Task.WhenAll( task1, task2 ).WaitWithTimeoutAsync();
    }

    #endregion

    #region Timeout Tests

    [Fact]
    public async Task Acquire_Timeout_ReturnsFalse()
    {
        var strategy = new LocalLockingStrategy();
        var firstLockHeld = new TaskCompletionSource<bool>();
        var testCompleted = new TaskCompletionSource<bool>();

        // First thread holds the lock
        var task1 = Task.Run(
            async () =>
            {
                using var lockHandle = strategy.GetLock( _testKey );
                lockHandle.Acquire( TimeSpan.FromSeconds( 10 ), CancellationToken.None );

                try
                {
                    firstLockHeld.SetResult( true );
                    await testCompleted.Task.WaitWithTimeoutAsync();
                }
                finally
                {
                    lockHandle.Release();
                }
            } );

        await firstLockHeld.Task.WaitWithTimeoutAsync();

        // Second thread should timeout
        using var lockHandle2 = strategy.GetLock( _testKey );
        var acquired = lockHandle2.Acquire( TimeSpan.FromMilliseconds( 50 ), CancellationToken.None );

        Assert.False( acquired );

        testCompleted.SetResult( true );
        await task1.WaitWithTimeoutAsync();
    }

    [Fact]
    public async Task AcquireAsync_Timeout_ReturnsFalse()
    {
        var strategy = new LocalLockingStrategy();
        var firstLockHeld = new TaskCompletionSource<bool>();
        var testCompleted = new TaskCompletionSource<bool>();

        // First thread holds the lock
        var task1 = Task.Run(
            async () =>
            {
                using var lockHandle = strategy.GetLock( _testKey );
                await lockHandle.AcquireAsync( TimeSpan.FromSeconds( 10 ), CancellationToken.None );

                try
                {
                    firstLockHeld.SetResult( true );
                    await testCompleted.Task.WaitWithTimeoutAsync();
                }
                finally
                {
                    await lockHandle.ReleaseAsync();
                }
            } );

        await firstLockHeld.Task.WaitWithTimeoutAsync();

        // Second thread should timeout
        using var lockHandle2 = strategy.GetLock( _testKey );
        var acquired = await lockHandle2.AcquireAsync( TimeSpan.FromMilliseconds( 50 ), CancellationToken.None );

        Assert.False( acquired );

        testCompleted.SetResult( true );
        await task1.WaitWithTimeoutAsync();
    }

    #endregion

    #region Reference Counting Tests

    [Fact]
    public void GetLock_SameKey_SharesUnderlyingLock()
    {
        var strategy = new LocalLockingStrategy();

        // Get two handles for the same key
        using var handle1 = strategy.GetLock( _testKey );
        using var handle2 = strategy.GetLock( _testKey );

        // Both should be valid
        Assert.NotNull( handle1 );
        Assert.NotNull( handle2 );

        // Acquire on handle1
        var acquired1 = handle1.Acquire( TimeSpan.FromSeconds( 1 ), CancellationToken.None );
        Assert.True( acquired1 );

        // handle2 should not be able to acquire (same underlying lock)
        var acquired2 = handle2.Acquire( TimeSpan.FromMilliseconds( 10 ), CancellationToken.None );
        Assert.False( acquired2 );

        handle1.Release();

        // Now handle2 should succeed
        acquired2 = handle2.Acquire( TimeSpan.FromSeconds( 1 ), CancellationToken.None );
        Assert.True( acquired2 );
        handle2.Release();
    }

    [Fact]
    public void Dispose_AllHandles_RemovesLockFromDictionary()
    {
        var strategy = new LocalLockingStrategy();

        // Get a handle and dispose it
        var handle = strategy.GetLock( _testKey );
        var acquired = handle.Acquire( TimeSpan.FromSeconds( 1 ), CancellationToken.None );
        Assert.True( acquired );
        handle.Release();
        handle.Dispose();

        // Getting a new handle should work (new lock created)
        using var newHandle = strategy.GetLock( _testKey );
        var newAcquired = newHandle.Acquire( TimeSpan.FromSeconds( 1 ), CancellationToken.None );
        Assert.True( newAcquired );
        newHandle.Release();
    }

    #endregion

    #region Concurrent Operations Tests

    [Fact]
    public async Task ConcurrentAcquireRelease_SameKey_NoDataRace()
    {
        var strategy = new LocalLockingStrategy();
        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var counter = 0;
        var errors = 0;

        var tasks = new Task[threadCount];

        for ( var t = 0; t < threadCount; t++ )
        {
            tasks[t] = Task.Run(
                () =>
                {
                    for ( var i = 0; i < iterationsPerThread; i++ )
                    {
                        using var lockHandle = strategy.GetLock( _testKey );
                        var acquired = lockHandle.Acquire( TimeSpan.FromSeconds( 10 ), CancellationToken.None );

                        if ( acquired )
                        {
                            try
                            {
                                // Critical section - should be mutually exclusive
                                var current = Interlocked.Increment( ref counter );

                                if ( current != 1 )
                                {
                                    Interlocked.Increment( ref errors );
                                }

                                // Small delay to increase chance of race condition
                                Thread.SpinWait( 100 );

                                current = Interlocked.Decrement( ref counter );

                                if ( current != 0 )
                                {
                                    Interlocked.Increment( ref errors );
                                }
                            }
                            finally
                            {
                                lockHandle.Release();
                            }
                        }
                    }
                } );
        }

        await Task.WhenAll( tasks ).WaitWithTimeoutAsync( timeout: TimeSpan.FromMinutes( 1 ) );

        Assert.Equal( 0, errors );
        Assert.Equal( 0, counter );
    }

    [Fact]
    public async Task ConcurrentAcquireRelease_DifferentKeys_IndependentProgress()
    {
        var strategy = new LocalLockingStrategy();
        const int keyCount = 5;
        const int operationsPerKey = 50;
        var completedOperations = new int[keyCount];

        var tasks = new Task[keyCount];

        for ( var k = 0; k < keyCount; k++ )
        {
            var keyIndex = k;
            var key = $"key{keyIndex}";

            tasks[k] = Task.Run(
                async () =>
                {
                    for ( var i = 0; i < operationsPerKey; i++ )
                    {
                        using var lockHandle = strategy.GetLock( key );
                        var acquired = await lockHandle.AcquireAsync( TimeSpan.FromSeconds( 10 ), CancellationToken.None );

                        if ( acquired )
                        {
                            try
                            {
                                Interlocked.Increment( ref completedOperations[keyIndex] );
                            }
                            finally
                            {
                                await lockHandle.ReleaseAsync();
                            }
                        }
                    }
                } );
        }

        await Task.WhenAll( tasks ).WaitWithTimeoutAsync( timeout: TimeSpan.FromMinutes( 1 ) );

        // All operations for all keys should complete
        for ( var k = 0; k < keyCount; k++ )
        {
            Assert.Equal( operationsPerKey, completedOperations[k] );
        }
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WithoutRelease_ThrowsInvalidOperationException()
    {
        var strategy = new LocalLockingStrategy();

        var handle = strategy.GetLock( _testKey );
        var acquired = handle.Acquire( TimeSpan.FromSeconds( 1 ), CancellationToken.None );
        Assert.True( acquired );

        // Disposing without releasing should throw
        Assert.Throws<InvalidOperationException>( () => handle.Dispose() );

        // Cleanup
        handle.Release();
        handle.Dispose();
    }

    [Fact]
    public void DoubleDispose_DoesNotThrow()
    {
        var strategy = new LocalLockingStrategy();

        var handle = strategy.GetLock( _testKey );
        handle.Dispose();

        // Second dispose should not throw
        handle.Dispose();
    }

    [Fact]
    public void Release_WithoutAcquire_DoesNotThrow()
    {
        var strategy = new LocalLockingStrategy();

        using var handle = strategy.GetLock( _testKey );

        // Release without acquire should not throw
        handle.Release();
    }

    [Fact]
    public void DoubleAcquire_ThrowsInvalidOperationException()
    {
        var strategy = new LocalLockingStrategy();

        using var handle = strategy.GetLock( _testKey );
        var acquired = handle.Acquire( TimeSpan.FromSeconds( 1 ), CancellationToken.None );
        Assert.True( acquired );

        // Second acquire should throw
        Assert.Throws<InvalidOperationException>( () => handle.Acquire( TimeSpan.FromSeconds( 1 ), CancellationToken.None ) );

        handle.Release();
    }

    #endregion

    #region Case Insensitivity Tests

    [Fact]
    public void GetLock_CaseInsensitiveKeys_SharesLock()
    {
        var strategy = new LocalLockingStrategy();

        using var handle1 = strategy.GetLock( "TestKey" );
        using var handle2 = strategy.GetLock( "testkey" );
        using var handle3 = strategy.GetLock( "TESTKEY" );

        // All handles should share the same underlying lock
        var acquired1 = handle1.Acquire( TimeSpan.FromSeconds( 1 ), CancellationToken.None );
        Assert.True( acquired1 );

        // Other handles should not be able to acquire (same underlying lock)
        var acquired2 = handle2.Acquire( TimeSpan.FromMilliseconds( 10 ), CancellationToken.None );
        Assert.False( acquired2 );

        var acquired3 = handle3.Acquire( TimeSpan.FromMilliseconds( 10 ), CancellationToken.None );
        Assert.False( acquired3 );

        handle1.Release();
    }

    #endregion
}