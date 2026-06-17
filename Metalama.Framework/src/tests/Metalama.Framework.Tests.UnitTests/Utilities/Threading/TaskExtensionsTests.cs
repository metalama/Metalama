// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Threading;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities.Threading;

#pragma warning disable VSTHRD200 // Use "Async" suffix - test naming convention prefers descriptive names.

public sealed class TaskExtensionsTests
{
    /// <summary>
    /// Regression test for issue #1624 (Fix 1B). When the cancellation token of a
    /// <c>WithCancellation</c>-wrapped task fires, the resulting cancellation continuations must
    /// not execute inline on the thread that calls <c>CancellationTokenSource.Cancel</c>. If they
    /// did, locks held elsewhere by that thread (or that it is supposed to release later) would be
    /// unreachable, producing the deadlock observed in CI. The invariant is enforced by passing
    /// <see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> to the internal
    /// <see cref="TaskCompletionSource{T}"/> used by <c>WithCancellationSlow</c>.
    /// </summary>
    /// <remarks>
    /// The test asserts the invariant <em>directly and deterministically</em>: it captures the
    /// thread on which the continuation actually executes and proves it is not the cancelling
    /// thread. There is no timing race — a <see cref="TaskCompletionSource{T}"/> signalled from
    /// inside the continuation is the synchronization point.
    ///
    /// <para><c>ExecuteSynchronously</c> is essential: it forces the continuation to ride whichever
    /// thread completes <c>wrapped</c>. Without <c>RunContinuationsAsynchronously</c> that thread is
    /// the cancelling thread (the whole async cascade runs inline inside <c>Cancel</c>), so the
    /// captured thread would equal the cancelling thread and the assertion fails. With the flag in
    /// place the cascade is dispatched to the thread pool, so the captured thread is a pool worker —
    /// never the dedicated, non-pool cancelling <see cref="Thread"/>.</para>
    /// </remarks>
    [Fact]
    public async Task WithCancellation_OnCancel_DoesNotRunContinuationsOnCancellingThread()
    {
        // A task that never completes on its own: the only way out of WithCancellation is the token.
        var hangingTask = new TaskCompletionSource<int>().Task;

        using var cts = new CancellationTokenSource();
        var wrapped = hangingTask.WithCancellation( cts.Token );

        // Signalled from inside the continuation with the thread it ran on. RunContinuationsAsynchronously
        // keeps the awaiting test thread from resuming inline, so it cannot perturb the measurement.
        var continuationRan = new TaskCompletionSource<Thread>( TaskCreationOptions.RunContinuationsAsynchronously );

        // ExecuteSynchronously makes the continuation run on whichever thread completes wrapped. If
        // Fix 1B regressed, that is the cancelling thread; with the fix it is a thread-pool worker.
        _ = wrapped.ContinueWith(
            _ => continuationRan.SetResult( Thread.CurrentThread ),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default );

        Thread? cancellingThread = null;
        var cancelThread = new Thread(
            () =>
            {
                cancellingThread = Thread.CurrentThread;
#pragma warning disable VSTHRD103 // Synchronous Cancel is exactly what this test exercises.
                cts.Cancel();
#pragma warning restore VSTHRD103
            } ) { IsBackground = true, Name = "WithCancellation-cancel-thread" };

        cancelThread.Start();

        // Unbounded join: establishes a happens-before for reading cancellingThread and guarantees
        // the cancel thread is gone before we compare identities. Cancel returns promptly in both
        // the fixed and regressed cases, so this never hangs.
        cancelThread.Join();

        // Deterministic sync point: completes exactly when the continuation has executed.
        var continuationThread = await continuationRan.Task;

        Assert.NotSame( cancellingThread, continuationThread );
    }
}
