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
    /// The cancellation runs on a dedicated non-thread-pool <see cref="Thread"/> rather than the
    /// xUnit test thread. Otherwise the test thread, after yielding at the post-Cancel await, can
    /// itself be reused by the thread pool to dispatch the queued continuation — producing a
    /// false match between "cancelling thread" and "continuation thread" even when the invariant
    /// holds. A dedicated thread is never a thread-pool worker, so its identity is distinct.
    /// </remarks>
    [Fact]
    public void WithCancellation_OnCancel_DoesNotRunContinuationsInline()
    {
        // A task that never completes on its own: the only way out of WithCancellation is the token.
        var hangingTask = new TaskCompletionSource<int>().Task;

        using var cts = new CancellationTokenSource();
        var wrapped = hangingTask.WithCancellation( cts.Token );

        Thread? continuationThread = null;
        using var continuationDone = new ManualResetEventSlim( false );

        // Request synchronous continuation: this is the strongest possible "run on the completing
        // thread" hint a consumer can give. With RunContinuationsAsynchronously on the internal
        // TCS, the runtime must still honor asynchronous scheduling and not run the chain inline
        // on the thread that called Cancel.
        _ = wrapped.ContinueWith(
            _ =>
            {
                continuationThread = Thread.CurrentThread;
                continuationDone.Set();
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default );

        Thread? cancellingThread = null;
        var cancelThread = new Thread(
            () =>
            {
                cancellingThread = Thread.CurrentThread;
#pragma warning disable VSTHRD103 // Synchronous Cancel is exactly what this test measures.
                cts.Cancel();
#pragma warning restore VSTHRD103
            } ) { IsBackground = true, Name = "WithCancellation-cancel-thread" };

        cancelThread.Start();
        Assert.True( cancelThread.Join( TimeSpan.FromSeconds( 10 ) ), "Cancel thread should complete promptly." );
        Assert.True( continuationDone.Wait( TimeSpan.FromSeconds( 10 ) ), "Continuation should run." );

        Assert.NotNull( cancellingThread );
        Assert.NotNull( continuationThread );

        // The continuation must not have run on the dedicated cancelling thread. With Fix 1B's
        // RunContinuationsAsynchronously the chain dispatches to a thread-pool worker; without it
        // the entire async cascade runs inline on the cancelling thread and these references match.
        Assert.NotSame( cancellingThread, continuationThread );
    }
}
