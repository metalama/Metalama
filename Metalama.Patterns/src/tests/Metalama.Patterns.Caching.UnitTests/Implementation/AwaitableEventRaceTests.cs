// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Implementation;

/// <summary>
/// Deterministic reproductions of concurrency races in <see cref="AwaitableEvent"/>, driven through its
/// synchronization points via an injected <see cref="TestSynchronizationProvider"/>.
/// </summary>
/// <remarks>
/// These target the race behind the flaky test metalama/Metalama#1714: the caching back-end's dispose path
/// awaits <c>BackgroundTaskScheduler.WhenBackgroundTasksCompleted</c>, which is built on
/// <see cref="AwaitableEvent"/>. A stress loop of "enqueue one task, then await completion" hangs or crashes
/// within seconds; the tests below pin down one exact interleaving that misbehaves.
/// </remarks>
public sealed class AwaitableEventRaceTests
{
    private readonly ITestOutputHelper _output;

    public AwaitableEventRaceTests( ITestOutputHelper output )
    {
        this._output = output;
    }

    /// <summary>
    /// Reproduces a double-activation: when <c>Set</c> activates a scheduled async wait operation while the
    /// scheduling thread is between "the event is not signaled" and its <c>CREATED-&gt;WAITING</c> transition,
    /// the scheduling thread's failed CAS drives a second <c>Activate</c>, so the continuation is scheduled twice.
    /// In the real back-end that second scheduling re-runs an async state machine that has already completed,
    /// which throws "attempt to transition a task to a final state when it had already completed" on a
    /// thread-pool thread (and can leave the dispose wait hung).
    /// </summary>
    [Fact( Timeout = 30000 )]
    public Task ManualReset_SetRacesScheduleContinuation_ActivatesContinuationOnce()
    {
        // Run the blocking orchestration off the test thread so the xunit Timeout can abort a hang (lost wakeup).
        return Task.Run( this.ManualReset_SetRacesScheduleContinuation_Core );
    }

    private void ManualReset_SetRacesScheduleContinuation_Core()
    {
        using var syncProvider = new TestSynchronizationProvider();

        var awaitableEvent = new AwaitableEvent( EventResetMode.ManualReset, syncProvider );
        var continuationCount = 0;

        var awaiter = awaitableEvent.WaitAsync();
        Assert.False( awaiter.IsCompleted );

        // Arm a pause immediately before the CREATED->WAITING transition in ScheduleContinuationInner.
        var syncPoint = syncProvider.Arm( "Event is not signaled, begin to wait." );

        // The scheduling thread will block at the sync point while inside OnCompleted.
        var schedulingThread = new Thread(
            () => awaiter.OnCompleted( () => Interlocked.Increment( ref continuationCount ) ) )
        {
            IsBackground = true, Name = "ScheduleContinuation"
        };

        schedulingThread.Start();

        Assert.True(
            syncPoint.WaitUntilReached( TimeSpan.FromSeconds( 10 ) ),
            "The scheduling thread did not reach the synchronization point." );

        // While the scheduling thread is paused, signal the event. This activates the enqueued operation
        // (scheduling the continuation once) before the scheduling thread resumes.
        awaitableEvent.Set();

        // Let the scheduling thread run its (now stale) CREATED->WAITING CAS and the branch that follows.
        syncPoint.Release();
        Assert.True( schedulingThread.Join( TimeSpan.FromSeconds( 10 ) ), "Scheduling thread did not complete." );

        // Wait for the first continuation, then allow time for an (erroneous) second one to arrive.
        Assert.True(
            SpinWait.SpinUntil( () => Volatile.Read( ref continuationCount ) >= 1, TimeSpan.FromSeconds( 5 ) ),
            "The continuation was never scheduled (lost wakeup)." );

        Thread.Sleep( 500 );

        var finalCount = Volatile.Read( ref continuationCount );
        this._output.WriteLine( $"Continuation ran {finalCount} time(s)." );

        Assert.Equal( 1, finalCount );
    }

    /// <summary>
    /// End-to-end guard mirroring the caching back-end's dispose path (enqueue a background task, then await
    /// <see cref="BackgroundTaskScheduler.WhenBackgroundTasksCompleted"/>). Before the fix this loop hung (lost
    /// wakeup) or crashed (double-completion) within seconds; after the fix it drains cleanly.
    /// </summary>
    /// <remarks>
    /// Load test: excluded from CI (it runs hundreds of thousands of iterations). Run manually to stress the
    /// manual-reset Set/await handshake, ideally under CPU saturation.
    /// </remarks>
    [Fact( Timeout = 60000, Skip = "Load test - run manually (see remarks)." )]
    public async Task WhenBackgroundTasksCompleted_EnqueueThenAwait_NeverHangs()
    {
        using var scheduler = new BackgroundTaskScheduler( null );

        for ( var i = 0; i < 200_000; i++ )
        {
            scheduler.EnqueueBackgroundTask( _ => Task.CompletedTask );

            var completed = scheduler.WhenBackgroundTasksCompleted( CancellationToken.None );

            Assert.True(
                await Task.WhenAny( completed, Task.Delay( TimeSpan.FromSeconds( 10 ) ) ) == completed,
                $"WhenBackgroundTasksCompleted did not complete at iteration {i}." );
        }

        await scheduler.DisposeAsync();
    }
}
