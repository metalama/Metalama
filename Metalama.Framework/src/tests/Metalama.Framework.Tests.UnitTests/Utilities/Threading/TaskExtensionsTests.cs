// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Threading;
using System;
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
    /// The test asserts the semantic invariant directly: a blocking continuation should not stop
    /// <c>Cancel</c> from returning. Without <c>RunContinuationsAsynchronously</c> the continuation
    /// runs inline on the cancelling thread, so <c>Cancel</c> is blocked inside the continuation
    /// waiting on the same event the test only releases after the timeout has passed. With the flag
    /// in place the continuation is dispatched to the thread pool and <c>Cancel</c> returns
    /// immediately.
    ///
    /// Cancellation is started on a dedicated non-thread-pool <see cref="Thread"/> so that
    /// <c>Join</c> measures Cancel's progress directly (a thread-pool task would let the thread pool
    /// schedule it however it wants, complicating the timing semantics).
    /// </remarks>
    [Fact]
    public void WithCancellation_OnCancel_DoesNotBlockOnSlowContinuations()
    {
        // A task that never completes on its own: the only way out of WithCancellation is the token.
        var hangingTask = new TaskCompletionSource<int>().Task;

        using var cts = new CancellationTokenSource();
        var wrapped = hangingTask.WithCancellation( cts.Token );

        using var continuationCanProceed = new ManualResetEventSlim( false );

        // The continuation blocks on continuationCanProceed. With ExecuteSynchronously requested,
        // an inline-running continuation would freeze the thread that completes wrapped — which,
        // if Fix 1B regressed, is exactly the cancelling thread.
        var continued = wrapped.ContinueWith(
            _ => continuationCanProceed.Wait(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default );

        var cancelThread = new Thread(
            () =>
            {
#pragma warning disable VSTHRD103 // Synchronous Cancel is exactly what this test measures.
                cts.Cancel();
#pragma warning restore VSTHRD103
            } ) { IsBackground = true, Name = "WithCancellation-cancel-thread" };

        cancelThread.Start();

        // Primary invariant. With Fix 1B's RunContinuationsAsynchronously the chain dispatches to a
        // thread-pool worker and the cancelling thread returns immediately. Without it, the entire
        // async cascade runs inline on the cancelling thread and gets stuck inside the continuation
        // until we Set the event below — which we delay until after this Join's timeout. So the
        // bug regressing manifests as Join returning false.
        var cancelReturned = cancelThread.Join( TimeSpan.FromSeconds( 2 ) );

        // Release the continuation so the thread pool (or, in the regressed case, the cancel thread)
        // unwinds before the test exits. Otherwise we'd leak a blocked worker.
        continuationCanProceed.Set();

        Assert.True( cancelReturned, "Cancel must return without waiting on the continuation; got blocked instead." );
        Assert.True( continued.Wait( TimeSpan.FromSeconds( 5 ) ), "Continuation should complete after being released." );
    }
}
