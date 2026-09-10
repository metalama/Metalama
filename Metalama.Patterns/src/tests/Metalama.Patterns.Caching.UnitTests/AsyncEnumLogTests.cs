// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NETCOREAPP3_0_OR_GREATER
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests;

/// <summary>
/// Tests the log that <see cref="AsyncEnumTestsBase"/> shares between the test thread and the tasks that
/// the caching aspect starts.
/// </summary>
/// <remarks>
/// These are regression tests for issue 2009. The log was read without synchronization while a task started by
/// the caching aspect was still appending to it, which made every test class derived from
/// <see cref="AsyncEnumTestsBase"/> fail intermittently.
/// </remarks>
public sealed class AsyncEnumLogTests : AsyncEnumTestsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncEnumLogTests"/> class.
    /// </summary>
    public AsyncEnumLogTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }

    /// <summary>
    /// Verifies that the log can be read while another task appends to it.
    /// </summary>
    /// <remarks>
    /// The test reproduces the failure of issue 2009: <see cref="System.Text.StringBuilder"/> is not thread-safe,
    /// so reading it while another thread appends to it throws <see cref="ArgumentOutOfRangeException"/> for the
    /// <c>chunkLength</c> parameter.
    /// </remarks>
    [Fact]
    public async Task ReadingTheLogWhileAnotherTaskAppendsIsSafe()
    {
        const int appendCount = 50_000;

        using var cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromMinutes( 1 ) );
        var cancellationToken = cancellationTokenSource.Token;

        // The appending task starts only once the reading loop below is running, so that the two overlap.
        var readerIsRunning = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        var appendTask = Task.Run(
            async () =>
            {
                await readerIsRunning.Task.WaitAsync( cancellationToken );

                for ( var i = 0; i < appendCount; i++ )
                {
                    this.Log( "E" );
                }
            },
            cancellationToken );

        readerIsRunning.SetResult( true );

        while ( !appendTask.IsCompleted )
        {
            cancellationToken.ThrowIfCancellationRequested();

            _ = this.GetLog();

            // The loop yields after every read, so that it does not hold a thread of the pool for the whole
            // duration of the appending task. The reads still overlap the appends, because the two tasks run
            // concurrently and the appending task performs many appends between two reads.
            await Task.Yield();
        }

        await appendTask.WaitAsync( cancellationToken );
    }

    /// <summary>
    /// Verifies that the log contains the whole sequence of the blocked enumeration once the blocking task has
    /// been released.
    /// </summary>
    /// <remarks>
    /// Before the correction, releasing the blocking task did not wait for the enumeration that it unblocks, so
    /// the log read by the disposal of the test was truncated and the entries missing from it were appended while
    /// the log was being read.
    /// </remarks>
    [Fact]
    public async Task TheLogIsCompleteWhenTheBlockedEnumerationHasBeenReleased()
    {
        using var cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromMinutes( 1 ) );

        // ReSharper disable once NotDisposedResource
        _ = this.BlockedCachedEnumerable().GetAsyncEnumerator();

        await this.FinishBlockingTaskAsync( cancellationTokenSource.Token );

        Assert.Equal( "E1.E2.E3.E4", this.GetLog() );
    }
}

#endif
