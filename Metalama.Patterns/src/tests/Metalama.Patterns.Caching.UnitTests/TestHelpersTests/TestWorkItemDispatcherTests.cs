// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.TestHelpers;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.TestHelpersTests;

/// <summary>
/// Tests of the waits of <see cref="TestWorkItemDispatcher"/>.
/// </summary>
public sealed class TestWorkItemDispatcherTests
{
    /// <summary>
    /// Verifies that the cancellation of one wait leaves the other waits of the same period of activity intact,
    /// including a wait that is started after the cancellation.
    /// </summary>
    [Fact]
    public async Task CancellingOneWait_LeavesTheOtherWaitsIntact()
    {
        var dispatcher = new TestWorkItemDispatcher();

        using var workItemReleased = new ManualResetEventSlim( false );
        using var cancellationTokenSource = new CancellationTokenSource();
        using var testCancellationTokenSource = new CancellationTokenSource();

        dispatcher.Dispatch( _ => workItemReleased.Wait(), null );

        var cancelledWait = dispatcher.WhenPendingWorkItemsCompletedAsync( cancellationTokenSource.Token );
        var concurrentWait = dispatcher.WhenPendingWorkItemsCompletedAsync( testCancellationTokenSource.Token );

        Task laterWait;

        try
        {
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>( () => cancelledWait );

            laterWait = dispatcher.WhenPendingWorkItemsCompletedAsync( testCancellationTokenSource.Token );

            // The work item is still blocked, so neither of the two remaining waits can have completed.
            Assert.False( concurrentWait.IsCompleted );
            Assert.False( laterWait.IsCompleted );
            Assert.Equal( 1, dispatcher.PendingWorkItemCount );
        }
        finally
        {
            workItemReleased.Set();
        }

        await concurrentWait;
        await laterWait;

        Assert.Equal( 0, dispatcher.PendingWorkItemCount );
    }

    /// <summary>
    /// Verifies that a wait started with a token that is already cancelled is cancelled, and that the work items
    /// still complete afterwards.
    /// </summary>
    [Fact]
    public async Task WaitingWithACancelledToken_IsCancelled()
    {
        var dispatcher = new TestWorkItemDispatcher();

        using var workItemReleased = new ManualResetEventSlim( false );
        using var cancellationTokenSource = new CancellationTokenSource();
        using var testCancellationTokenSource = new CancellationTokenSource();

        dispatcher.Dispatch( _ => workItemReleased.Wait(), null );

        cancellationTokenSource.Cancel();

        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => dispatcher.WhenPendingWorkItemsCompletedAsync( cancellationTokenSource.Token ) );
        }
        finally
        {
            workItemReleased.Set();
        }

        await dispatcher.WhenPendingWorkItemsCompletedAsync( testCancellationTokenSource.Token );
    }
}
