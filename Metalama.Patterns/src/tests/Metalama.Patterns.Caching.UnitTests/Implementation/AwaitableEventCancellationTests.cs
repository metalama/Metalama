// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Implementation;

/// <summary>
/// Tests that cancelling a blocked synchronous <see cref="AwaitableEvent"/> wait withdraws the operation from the
/// wait protocol (WAITING -> TIMEOUT) instead of abandoning it in the queue. An abandoned operation would let a
/// later <c>Set()</c> spuriously fire the shared thread-static event and wake an unrelated wait, or (for
/// auto-reset) permanently eat a signal. These are driven deterministically through the event's synchronization
/// points via an injected <see cref="TestSynchronizationProvider"/> - no timing delays.
/// </summary>
public sealed class AwaitableEventCancellationTests
{
    // "Signal not observed, wait." is emitted only by WaitManualReset, immediately before it blocks on the event.
    private const string _manualPreBlock = "Signal not observed, wait.";

    // "Signal not taken, wait." is emitted by WaitAutoReset before it blocks (and also by the async path, which
    // these purely synchronous tests never exercise).
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
            "The waiter did not reach the pre-block sync point. Is the back-end built with DEBUG?" );

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
}
