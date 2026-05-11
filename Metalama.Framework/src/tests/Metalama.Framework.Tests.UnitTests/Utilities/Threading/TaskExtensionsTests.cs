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
    [Fact]
    public async Task WithCancellation_OnCancel_DoesNotRunContinuationsInline()
    {
        // A task that never completes on its own: the only way out of WithCancellation is the token.
        var hangingTask = new TaskCompletionSource<int>().Task;

        using var cts = new CancellationTokenSource();
        var wrapped = hangingTask.WithCancellation( cts.Token );

        var cancellingThreadId = Thread.CurrentThread.ManagedThreadId;
        var continuationThreadId = 0;
        var continuationFinishedTcs = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        // Request inline continuation: this is the strongest possible "run on the cancelling
        // thread" hint a consumer could give. With RunContinuationsAsynchronously on the internal
        // TCS, the runtime is required to honor asynchronous scheduling regardless.
        _ = wrapped.ContinueWith(
            _ =>
            {
                continuationThreadId = Thread.CurrentThread.ManagedThreadId;
                continuationFinishedTcs.TrySetResult( true );
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default );

#pragma warning disable VSTHRD103 // Synchronous Cancel is exactly what this test measures.
        cts.Cancel();
#pragma warning restore VSTHRD103

        await continuationFinishedTcs.Task;

        Assert.NotEqual( 0, continuationThreadId );
        Assert.NotEqual( cancellingThreadId, continuationThreadId );
    }
}
