// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Implementation;

public sealed class AwaitableEventTests
{
    private readonly ITestOutputHelper _testOutput;

    public AwaitableEventTests( ITestOutputHelper testOutputHelper )
    {
        this._testOutput = testOutputHelper;
    }

    #region Manual Reset Mode Tests

    [Fact]
    public void ManualReset_Set_StaysSignaled()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset );

        // Initially not signaled
        Assert.Equal( AwaitableEvent.NOT_SIGNALED, awaitableEvent.SignalState );

        // Set should make it signaled
        awaitableEvent.Set();
        Assert.Equal( AwaitableEvent.SIGNALED, awaitableEvent.SignalState );

        // Calling Set again should keep it signaled
        awaitableEvent.Set();
        Assert.Equal( AwaitableEvent.SIGNALED, awaitableEvent.SignalState );
    }

    [Fact]
    public void ManualReset_Wait_DoesNotResetSignal()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset, signaled: true );

        // Wait should return immediately for a signaled event
        var result = awaitableEvent.Wait( TimeSpan.Zero );
        Assert.True( result );

        // Manual reset should stay signaled after wait
        Assert.Equal( AwaitableEvent.SIGNALED, awaitableEvent.SignalState );

        // Second wait should also succeed
        result = awaitableEvent.Wait( TimeSpan.Zero );
        Assert.True( result );
    }

    [Fact]
    public async Task ManualReset_MultipleWaiters_AllReleasedOnSet()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset );
        const int waiterCount = 5;
        var waitersReleased = 0;
        var allWaitersStarted = new TaskCompletionSource<bool>();
        var waitersStarted = 0;
        var syncLock = new object();
        using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 10 ) );

        var waiterTasks = new Task[waiterCount];

        for ( var i = 0; i < waiterCount; i++ )
        {
            waiterTasks[i] = Task.Run(
                () =>
                {
                    lock ( syncLock )
                    {
                        waitersStarted++;

                        if ( waitersStarted == waiterCount )
                        {
                            allWaitersStarted.TrySetResult( true );
                        }
                    }

                    // This will block until signaled
                    awaitableEvent.Wait( cts.Token );
                    Interlocked.Increment( ref waitersReleased );
                } );
        }

        // Wait until all waiters have started
        await allWaitersStarted.Task.WaitWithTimeoutAsync();

        // Signal the event - all waiters should be released
        awaitableEvent.Set();

        await Task.WhenAll( waiterTasks ).WaitWithTimeoutAsync();

        Assert.Equal( waiterCount, waitersReleased );
    }

    [Fact]
    public void ManualReset_Reset_ClearsSignaledState()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset, signaled: true );
        Assert.Equal( AwaitableEvent.SIGNALED, awaitableEvent.SignalState );

        awaitableEvent.Reset();

        Assert.Equal( AwaitableEvent.NOT_SIGNALED, awaitableEvent.SignalState );
    }

    [Fact]
    public void ManualReset_Wait_OnUnsignaled_TimesOut()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset );

        var result = awaitableEvent.Wait( TimeSpan.FromMilliseconds( 10 ) );

        Assert.False( result );
    }

    #endregion

    #region Auto Reset Mode Tests

    [Fact]
    public void AutoReset_Wait_ConsumesSignal()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.AutoReset, signaled: true );

        // First wait should consume the signal
        var result = awaitableEvent.Wait( TimeSpan.Zero );
        Assert.True( result );

        // Should be not signaled after wait consumed it
        Assert.Equal( AwaitableEvent.NOT_SIGNALED, awaitableEvent.SignalState );

        // Second wait should fail since signal was consumed
        result = awaitableEvent.Wait( TimeSpan.Zero );
        Assert.False( result );
    }

    [Fact]
    public async Task AutoReset_SingleWaiter_ReleasedOnSet()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.AutoReset );
        var waiterReleased = new TaskCompletionSource<bool>();
        var waiterStarted = new TaskCompletionSource<bool>();
        using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 10 ) );

        var waiterTask = Task.Run(
            () =>
            {
                waiterStarted.SetResult( true );
                awaitableEvent.Wait( cts.Token );
                waiterReleased.SetResult( true );
            } );

        await waiterStarted.Task.WaitWithTimeoutAsync();

        // Signal the event
        awaitableEvent.Set();

        await waiterReleased.Task.WaitWithTimeoutAsync();
        await waiterTask.WaitWithTimeoutAsync();

        Assert.True( await waiterReleased.Task );
    }

    [Fact]
    public async Task AutoReset_MultipleWaiters_OnlyOneReleasedPerSet()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.AutoReset );
        const int waiterCount = 3;
        var waitersReleased = 0;
        var allWaitersStarted = new TaskCompletionSource<bool>();
        var firstWaiterReleased = new TaskCompletionSource<bool>();
        var waitersStarted = 0;
        var syncLock = new object();
        using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 10 ) );

        var waiterTasks = new Task[waiterCount];

        for ( var i = 0; i < waiterCount; i++ )
        {
            waiterTasks[i] = Task.Run(
                () =>
                {
                    lock ( syncLock )
                    {
                        waitersStarted++;

                        if ( waitersStarted == waiterCount )
                        {
                            allWaitersStarted.TrySetResult( true );
                        }
                    }

                    awaitableEvent.Wait( cts.Token );
                    var releasedCount = Interlocked.Increment( ref waitersReleased );

                    if ( releasedCount == 1 )
                    {
                        firstWaiterReleased.TrySetResult( true );
                    }
                } );
        }

        await allWaitersStarted.Task.WaitWithTimeoutAsync();

        // Signal once - only one waiter should be released
        awaitableEvent.Set();

        // Wait for exactly one waiter to be released
        await firstWaiterReleased.Task.WaitWithTimeoutAsync();

        // At this point exactly 1 waiter should be released
        var releasedAfterFirstSet = Volatile.Read( ref waitersReleased );
        this._testOutput.WriteLine( $"Waiters released after first Set: {releasedAfterFirstSet}" );
        Assert.Equal( 1, releasedAfterFirstSet );

        // Signal again for remaining waiters
        for ( var i = 1; i < waiterCount; i++ )
        {
            awaitableEvent.Set();
        }

        await Task.WhenAll( waiterTasks ).WaitWithTimeoutAsync();

        Assert.Equal( waiterCount, waitersReleased );
    }

    [Fact]
    public void AutoReset_SetWithNoWaiters_StaysSignaled()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.AutoReset );

        awaitableEvent.Set();

        // Should be signaled when no waiters
        Assert.Equal( AwaitableEvent.SIGNALED, awaitableEvent.SignalState );
    }

    #endregion

    #region Async Wait Tests

    [Fact]
    public async Task WaitAsync_AlreadySignaled_ReturnsImmediately()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset, signaled: true );

        var awaiter = awaitableEvent.WaitAsync();

        Assert.True( awaiter.IsCompleted );

        // Awaiter is already completed, so GetResult is non-blocking
#pragma warning disable xUnit1031
        Assert.True( awaiter.GetResult() );
#pragma warning restore xUnit1031
    }

    [Fact]
    public async Task WaitAsync_ZeroTimeout_PeekOperation()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset );

        var awaiter = awaitableEvent.WaitAsync( TimeSpan.Zero );

        Assert.True( awaiter.IsCompleted );

        // Awaiter is already completed, so GetResult is non-blocking
#pragma warning disable xUnit1031
        Assert.False( awaiter.GetResult() );
#pragma warning restore xUnit1031

        // Signal and try again
        awaitableEvent.Set();

        awaiter = awaitableEvent.WaitAsync( TimeSpan.Zero );

        Assert.True( awaiter.IsCompleted );

        // Awaiter is already completed, so GetResult is non-blocking
#pragma warning disable xUnit1031
        Assert.True( awaiter.GetResult() );
#pragma warning restore xUnit1031
    }

    [Fact]
    public async Task WaitAsync_AutoReset_ZeroTimeout_ConsumesSignal()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.AutoReset, signaled: true );

        var awaiter = awaitableEvent.WaitAsync( TimeSpan.Zero );

        Assert.True( awaiter.IsCompleted );

        // Awaiter is already completed, so GetResult is non-blocking
#pragma warning disable xUnit1031
        Assert.True( awaiter.GetResult() );
#pragma warning restore xUnit1031

        // Signal should be consumed
        Assert.Equal( AwaitableEvent.NOT_SIGNALED, awaitableEvent.SignalState );
    }

    [Fact]
    public async Task WaitAsync_ManualReset_ReleasedOnSet()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset );
        var waitCompleted = new TaskCompletionSource<bool>();

        var awaiter = awaitableEvent.WaitAsync();
        Assert.False( awaiter.IsCompleted );

        // Schedule continuation
        awaiter.OnCompleted( () => waitCompleted.SetResult( awaiter.GetResult() ) );

        // Signal
        awaitableEvent.Set();

        var result = await waitCompleted.Task.WaitWithTimeoutAsync();
        Assert.True( result );
    }

    [Fact]
    public async Task WaitAsync_AutoReset_ReleasedOnSet()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.AutoReset );
        var waitCompleted = new TaskCompletionSource<bool>();

        var awaiter = awaitableEvent.WaitAsync();
        Assert.False( awaiter.IsCompleted );

        // Schedule continuation
        awaiter.OnCompleted( () => waitCompleted.SetResult( awaiter.GetResult() ) );

        // Signal
        awaitableEvent.Set();

        var result = await waitCompleted.Task.WaitWithTimeoutAsync();
        Assert.True( result );
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ManualReset_NotSignaled_InitializesCorrectly()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset );

        Assert.Equal( AwaitableEvent.NOT_SIGNALED, awaitableEvent.SignalState );
        Assert.NotNull( awaitableEvent.Operations );
        Assert.True( awaitableEvent.Operations.IsEmpty );
    }

    [Fact]
    public void Constructor_ManualReset_Signaled_InitializesCorrectly()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset, signaled: true );

        Assert.Equal( AwaitableEvent.SIGNALED, awaitableEvent.SignalState );
    }

    [Fact]
    public void Constructor_AutoReset_NotSignaled_InitializesCorrectly()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.AutoReset );

        Assert.Equal( AwaitableEvent.NOT_SIGNALED, awaitableEvent.SignalState );
    }

    [Fact]
    public void Constructor_AutoReset_Signaled_InitializesCorrectly()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.AutoReset, signaled: true );

        Assert.Equal( AwaitableEvent.SIGNALED, awaitableEvent.SignalState );
    }

    #endregion

    #region Concurrent Operations Tests

    [Fact]
    public async Task ConcurrentSetReset_ManualReset_NoDeadlock()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset );
        const int iterations = 1000;
        using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );

        var setTask = Task.Run(
            () =>
            {
                for ( var i = 0; i < iterations && !cts.IsCancellationRequested; i++ )
                {
                    awaitableEvent.Set();
                }
            } );

        var resetTask = Task.Run(
            () =>
            {
                for ( var i = 0; i < iterations && !cts.IsCancellationRequested; i++ )
                {
                    awaitableEvent.Reset();
                }
            } );

        await Task.WhenAll( setTask, resetTask ).WaitWithTimeoutAsync( "ConcurrentSetReset deadlock" );
    }

    [Fact]
    public async Task ConcurrentSetWait_ManualReset_NoDataRace()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset );
        const int iterations = 100;
        var waitSuccessCount = 0;
        using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );

        var setTask = Task.Run(
            () =>
            {
                for ( var i = 0; i < iterations && !cts.IsCancellationRequested; i++ )
                {
                    awaitableEvent.Set();
                    awaitableEvent.Reset();
                }
            } );

        var waitTask = Task.Run(
            () =>
            {
                for ( var i = 0; i < iterations && !cts.IsCancellationRequested; i++ )
                {
                    if ( awaitableEvent.Wait( TimeSpan.FromMilliseconds( 1 ) ) )
                    {
                        Interlocked.Increment( ref waitSuccessCount );
                    }
                }
            } );

        await Task.WhenAll( setTask, waitTask ).WaitWithTimeoutAsync( "ConcurrentSetWait deadlock" );

        this._testOutput.WriteLine( $"Wait succeeded {waitSuccessCount} times out of {iterations}" );
    }

    [Fact]
    public async Task ConcurrentWaiters_AutoReset_CorrectWaiterCount()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.AutoReset );
        const int waiterCount = 10;
        var waitersReleased = 0;
        var allWaitersStarted = new TaskCompletionSource<bool>();
        var waitersStarted = 0;
        var syncLock = new object();
        using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );

        var waiterTasks = new Task[waiterCount];

        for ( var i = 0; i < waiterCount; i++ )
        {
            waiterTasks[i] = Task.Run(
                () =>
                {
                    lock ( syncLock )
                    {
                        waitersStarted++;

                        if ( waitersStarted == waiterCount )
                        {
                            allWaitersStarted.TrySetResult( true );
                        }
                    }

                    if ( awaitableEvent.Wait( TimeSpan.FromSeconds( 5 ) ) )
                    {
                        Interlocked.Increment( ref waitersReleased );
                    }
                } );
        }

        await allWaitersStarted.Task.WaitWithTimeoutAsync();

        // Signal exactly waiterCount times
        for ( var i = 0; i < waiterCount; i++ )
        {
            awaitableEvent.Set();
        }

        await Task.WhenAll( waiterTasks ).WaitWithTimeoutAsync();

        Assert.Equal( waiterCount, waitersReleased );
    }

    #endregion

    #region Awaiter with Data Tests

    [Fact]
    public async Task WaitAsyncWithData_AlreadySignaled_ReturnsImmediately()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset, signaled: true );

        var awaiter = awaitableEvent.WaitAsync<int>();

        Assert.True( awaiter.IsCompleted );
        Assert.True( awaiter.GetResult() );
    }

    [Fact]
    public async Task WaitAsyncWithData_ZeroTimeout_ReturnsCorrectResult()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset );

        var awaiter = awaitableEvent.WaitAsync<string>( TimeSpan.Zero );

        Assert.True( awaiter.IsCompleted );
        Assert.False( awaiter.GetResult() );
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public void Wait_Cancellation_ThrowsOperationCanceledException()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset );
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>( () => awaitableEvent.Wait( cts.Token ) );
    }

    [Fact]
    public void Wait_WithTimeout_Cancellation_ThrowsOperationCanceledException()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset );
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>( () => awaitableEvent.Wait( TimeSpan.FromSeconds( 5 ), cts.Token ) );
    }

    #endregion
}