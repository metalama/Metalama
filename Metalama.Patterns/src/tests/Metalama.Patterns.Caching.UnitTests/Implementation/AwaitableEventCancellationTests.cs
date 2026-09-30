// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Implementation;

/// <summary>
/// Tests that cancelling a blocked synchronous <see cref="AwaitableEvent"/> wait withdraws the operation from the
/// wait protocol (WAITING -> TIMEOUT) instead of abandoning it in the queue. An abandoned operation would let a
/// later <c>Set()</c> spuriously fire the shared thread-static event and wake an unrelated wait, or (for
/// auto-reset) permanently eat a signal. These are driven deterministically through the event's synchronization
/// points via an injected <see cref="TestSynchronizationProvider"/> - no timing delays.
/// The class also tests that cancelling a pending asynchronous wait completes the awaiter with an
/// <see cref="OperationCanceledException"/> and removes the operation from the wait queue.
/// </summary>
public sealed class AwaitableEventCancellationTests
{
    // "Signal not observed, wait." is emitted only by WaitManualReset, immediately before it blocks on the event.
    private const string _manualPreBlock = "Signal not observed, wait.";

    // "Signal not taken, wait." is emitted by WaitAutoReset before it blocks (and also by the async path, which
    // the synchronous tests never exercise).
    private const string _autoPreBlock = "Signal not taken, wait.";

    [Theory( Timeout = 30000 )]
    [InlineData( EventResetMode.ManualReset, _manualPreBlock )]
    [InlineData( EventResetMode.AutoReset, _autoPreBlock )]
    public async Task Wait_CancelledWhileBlocked_ThrowsAndLeavesEventUsable( EventResetMode mode, string preBlockMessage )
    {
        using var syncProvider = new TestSynchronizationProvider();

        var awaitableEvent = new AwaitableEvent( mode, syncProvider );
        using var cts = new CancellationTokenSource();

        // Pause the waiter right before it blocks on the event, while its operation is in the WAITING state.
        // The point is one-shot, so the fresh waiter below passes straight through it.
        var syncPoint = syncProvider.Arm( preBlockMessage );
        var waiterTask = Task.Run( () => awaitableEvent.Wait( cts.Token ) );

        Assert.True(
            syncPoint.WaitUntilReached( TimeSpan.FromSeconds( 10 ) ),
            "The waiter did not reach the pre-block synchronization point." );

        // Cancel, then let the waiter proceed into the (now cancelled) blocking wait.
        cts.Cancel();
        syncPoint.Release();

        await Assert.ThrowsAnyAsync<OperationCanceledException>( () => waiterTask );

        // The cancelled operation must have been withdrawn, so the event still works: Set() must release a fresh
        // waiter, and must not have been consumed by the abandoned operation.
        var released = new TaskCompletionSource<bool>();
        var freshWaiter = Task.Run( () => released.SetResult( awaitableEvent.Wait( TimeSpan.FromSeconds( 10 ) ) ), CancellationToken.None );

        awaitableEvent.Set();

        Assert.True( await released.Task, "The fresh waiter was not released by Set()." );
        await freshWaiter;
    }

    /// <summary>
    /// Tests that cancelling a pending asynchronous wait completes the awaiter with an <see cref="OperationCanceledException"/>
    /// and removes the operation from the wait queue, so that the operation does not consume a later signal.
    /// </summary>
    [Theory( Timeout = 30000 )]
    [InlineData( EventResetMode.ManualReset, false )]
    [InlineData( EventResetMode.AutoReset, false )]
    [InlineData( EventResetMode.ManualReset, true )]
    [InlineData( EventResetMode.AutoReset, true )]
    public async Task WaitAsync_CancelledWhilePending_ThrowsAndLeavesEventUsable( EventResetMode mode, bool withTimeout )
    {
        var awaitableEvent = new AwaitableEvent( mode );
        using var cts = new CancellationTokenSource();
        var waitCompleted = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        var awaiter = withTimeout ? awaitableEvent.WaitAsync( Timeout.InfiniteTimeSpan, cts.Token ) : awaitableEvent.WaitAsync( cts.Token );
        Assert.False( awaiter.IsCompleted );

        awaiter.OnCompleted(
            () =>
            {
                try
                {
                    waitCompleted.SetResult( awaiter.GetResult() );
                }
                catch ( Exception e )
                {
                    waitCompleted.SetException( e );
                }
            } );

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>( () => waitCompleted.Task.WaitWithTimeoutAsync() );

        // The cancelled operation must have been removed from the wait queue, so it must not consume the signal.
        awaitableEvent.Set();

        Assert.True( awaitableEvent.Wait( TimeSpan.Zero ), "The signal was consumed by the cancelled operation." );
    }

    /// <summary>
    /// Tests that cancelling a pending asynchronous wait removes the operation from the wait queue immediately, without
    /// a later <see cref="AwaitableEvent.Set"/>, so that repeated cancelled waits on an event that is never signaled do not
    /// accumulate in the queue.
    /// </summary>
    [Theory( Timeout = 30000 )]
    [InlineData( EventResetMode.ManualReset )]
    [InlineData( EventResetMode.AutoReset )]
    public async Task WaitAsync_CancelledWhilePending_RemovesOperationFromQueue( EventResetMode mode )
    {
        var awaitableEvent = new AwaitableEvent( mode );

        for ( var i = 0; i < 100; i++ )
        {
            using var cts = new CancellationTokenSource();
            var waitCompleted = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

            var awaiter = awaitableEvent.WaitAsync( cts.Token );
            Assert.False( awaiter.IsCompleted );

            awaiter.OnCompleted(
                () =>
                {
                    try
                    {
                        waitCompleted.SetResult( awaiter.GetResult() );
                    }
                    catch ( Exception e )
                    {
                        waitCompleted.SetException( e );
                    }
                } );

            Assert.Equal( 1, awaitableEvent.Operations.Count );

            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>( () => waitCompleted.Task.WaitWithTimeoutAsync() );

            Assert.True( awaitableEvent.Operations.IsEmpty, $"Iteration {i}: the cancelled operation remains in the queue." );
        }
    }

    /// <summary>
    /// Tests that cancelling a pending asynchronous wait that is queued behind another pending wait removes only the
    /// cancelled operation from the queue, and that a later <see cref="AwaitableEvent.Set"/> releases the other wait.
    /// </summary>
    [Theory( Timeout = 30000 )]
    [InlineData( EventResetMode.ManualReset )]
    [InlineData( EventResetMode.AutoReset )]
    public async Task WaitAsync_CancelledBehindPendingWait_RemovesOnlyCancelledOperation( EventResetMode mode )
    {
        var awaitableEvent = new AwaitableEvent( mode );
        using var cts = new CancellationTokenSource();
        var firstCompleted = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
        var secondCompleted = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        var firstAwaiter = awaitableEvent.WaitAsync();
        firstAwaiter.OnCompleted( () => firstCompleted.SetResult( firstAwaiter.GetResult() ) );

        var secondAwaiter = awaitableEvent.WaitAsync( cts.Token );

        secondAwaiter.OnCompleted(
            () =>
            {
                try
                {
                    secondCompleted.SetResult( secondAwaiter.GetResult() );
                }
                catch ( Exception e )
                {
                    secondCompleted.SetException( e );
                }
            } );

        Assert.Equal( 2, awaitableEvent.Operations.Count );

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>( () => secondCompleted.Task.WaitWithTimeoutAsync() );

        Assert.Equal( 1, awaitableEvent.Operations.Count );
        Assert.False( firstCompleted.Task.IsCompleted );

        awaitableEvent.Set();

        Assert.True( await firstCompleted.Task.WaitWithTimeoutAsync() );
        Assert.True( awaitableEvent.Operations.IsEmpty );
    }

    /// <summary>
    /// Tests that an asynchronous wait whose token is cancelled after the awaiter is created, but before the continuation
    /// is registered, completes exactly once with an <see cref="OperationCanceledException"/>. In this case, the
    /// cancellation callback runs synchronously when the continuation is registered.
    /// </summary>
    [Theory( Timeout = 30000 )]
    [InlineData( EventResetMode.ManualReset )]
    [InlineData( EventResetMode.AutoReset )]
    public async Task WaitAsync_CancelledBeforeContinuationRegistered_CompletesOnce( EventResetMode mode )
    {
        var awaitableEvent = new AwaitableEvent( mode );
        using var cts = new CancellationTokenSource();
        var waitCompleted = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
        var continuationCount = 0;

        var awaiter = awaitableEvent.WaitAsync( cts.Token );
        Assert.False( awaiter.IsCompleted );

        cts.Cancel();

        awaiter.OnCompleted(
            () =>
            {
                Interlocked.Increment( ref continuationCount );

                try
                {
                    waitCompleted.SetResult( awaiter.GetResult() );
                }
                catch ( Exception e )
                {
                    waitCompleted.SetException( e );
                }
            } );

        await Assert.ThrowsAnyAsync<OperationCanceledException>( () => waitCompleted.Task.WaitWithTimeoutAsync() );

        Assert.True( awaitableEvent.Operations.IsEmpty, "The cancelled operation remains in the queue." );

        // The cancelled operation must not be activated again, and must not consume the signal.
        awaitableEvent.Set();

        Assert.True( awaitableEvent.Wait( TimeSpan.Zero ), "The signal was consumed by the cancelled operation." );
        Assert.Equal( 1, continuationCount );
    }

    /// <summary>
    /// Tests that a <see cref="AwaitableEvent.Set"/> that races with the cancellation of a pending asynchronous wait on an
    /// auto-reset event delivers the signal exactly once: either the wait succeeds, or the signal remains on the event.
    /// </summary>
    [Fact( Timeout = 60000 )]
    public async Task WaitAsync_AutoReset_SetRacesWithCancel_SignalIsNotLost()
    {
        for ( var i = 0; i < 1000; i++ )
        {
            var awaitableEvent = new AwaitableEvent( EventResetMode.AutoReset );
            using var cts = new CancellationTokenSource();
            var waitCompleted = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

            var awaiter = awaitableEvent.WaitAsync( cts.Token );

            awaiter.OnCompleted(
                () =>
                {
                    try
                    {
                        waitCompleted.SetResult( awaiter.GetResult() );
                    }
                    catch ( OperationCanceledException )
                    {
                        waitCompleted.SetResult( false );
                    }
                } );

            using var barrier = new Barrier( 2 );

            var setTask = Task.Run(
                () =>
                {
                    barrier.SignalAndWait( CancellationToken.None );
                    awaitableEvent.Set();
                },
                CancellationToken.None );

            var cancelTask = Task.Run(
                () =>
                {
                    barrier.SignalAndWait( CancellationToken.None );
                    cts.Cancel();
                },
                CancellationToken.None );

            await Task.WhenAll( setTask, cancelTask ).WaitWithTimeoutAsync();

            var waitSucceeded = await waitCompleted.Task.WaitWithTimeoutAsync();
            var signalRemains = awaitableEvent.SignalState == AwaitableEvent.SIGNALED;

            Assert.True( waitSucceeded != signalRemains, $"Iteration {i}: waitSucceeded={waitSucceeded}, signalRemains={signalRemains}." );
        }
    }

    /// <summary>
    /// Tests that cancelling a pending asynchronous wait of the generic overload completes the awaiter with an
    /// <see cref="OperationCanceledException"/>.
    /// </summary>
    [Theory( Timeout = 30000 )]
    [InlineData( EventResetMode.ManualReset )]
    [InlineData( EventResetMode.AutoReset )]
    public async Task WaitAsyncWithData_CancelledWhilePending_Throws( EventResetMode mode )
    {
        var awaitableEvent = new AwaitableEvent( mode );
        using var cts = new CancellationTokenSource();
        var waitCompleted = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        var awaiter = awaitableEvent.WaitAsync<int>( cts.Token );
        Assert.False( awaiter.IsCompleted );

        awaiter.OnCompleted(
            _ =>
            {
                try
                {
                    waitCompleted.SetResult( awaiter.GetResult() );
                }
                catch ( Exception e )
                {
                    waitCompleted.SetException( e );
                }
            } );

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>( () => waitCompleted.Task.WaitWithTimeoutAsync() );
    }

    /// <summary>
    /// Tests that an asynchronous wait with a token that is already cancelled reports an <see cref="OperationCanceledException"/>.
    /// </summary>
    [Theory( Timeout = 30000 )]
    [InlineData( EventResetMode.ManualReset )]
    [InlineData( EventResetMode.AutoReset )]
    public async Task WaitAsync_AlreadyCancelled_Throws( EventResetMode mode )
    {
        var awaitableEvent = new AwaitableEvent( mode );
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () =>
            {
                var awaiter = awaitableEvent.WaitAsync( cts.Token );
                Assert.True( awaiter.IsCompleted );
                awaiter.GetResult();

                return Task.CompletedTask;
            } );

        Assert.Equal( AwaitableEvent.NOT_SIGNALED, awaitableEvent.SignalState );
    }

    /// <summary>
    /// Tests that an asynchronous wait with a cancellable token is released by <see cref="AwaitableEvent.Set"/>, and that
    /// cancelling the token after the release has no effect.
    /// </summary>
    [Fact( Timeout = 30000 )]
    public async Task WaitAsync_NotCancelled_ReleasedOnSet()
    {
        var awaitableEvent = new AwaitableEvent( EventResetMode.AutoReset );
        using var cts = new CancellationTokenSource();
        var waitCompleted = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        var awaiter = awaitableEvent.WaitAsync( cts.Token );
        Assert.False( awaiter.IsCompleted );

        awaiter.OnCompleted( () => waitCompleted.SetResult( awaiter.GetResult() ) );

        awaitableEvent.Set();

        Assert.True( await waitCompleted.Task.WaitWithTimeoutAsync() );

        cts.Cancel();

        Assert.Equal( AwaitableEvent.NOT_SIGNALED, awaitableEvent.SignalState );
    }
}
