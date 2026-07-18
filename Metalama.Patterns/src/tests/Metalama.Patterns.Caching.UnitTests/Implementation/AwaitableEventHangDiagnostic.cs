// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using System.Collections;
using System.Globalization;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Implementation;

/// <summary>
/// Load test / diagnostic for the manual-reset lost-wakeup that caused metalama/Metalama#1714. It hammers the
/// enqueue-then-await handshake of <see cref="BackgroundTaskScheduler"/> and, on a hang, reflects into the
/// <c>AwaitableEvent</c> to classify it as a genuine lost wakeup (op stuck in <c>WAITING</c> while the signal is
/// set) versus thread-pool starvation. Before the <c>Interlocked.MemoryBarrier</c> fence fix it stranded an
/// operation in <c>WAITING</c> forever (~1 run in 3); after the fix it drains cleanly, including under CPU load.
/// </summary>
/// <remarks>Excluded from CI (runs up to millions of iterations). Run manually, ideally under CPU saturation.</remarks>
public sealed class AwaitableEventHangDiagnostic
{
    private readonly ITestOutputHelper _output;

    public AwaitableEventHangDiagnostic( ITestOutputHelper output )
    {
        this._output = output;
    }

    [Fact( Timeout = 120000, Skip = "Load test - run manually (see remarks)." )]
    public async Task DumpStateOnHang()
    {
        using var scheduler = new BackgroundTaskScheduler( null );

        var schedulerType = typeof(BackgroundTaskScheduler);
        var countField = schedulerType.GetField( "_backgroundTaskCount", BindingFlags.NonPublic | BindingFlags.Instance )!;
        var eventField = schedulerType.GetField( "_backgroundTasksFinishedEvent", BindingFlags.NonPublic | BindingFlags.Instance )!;
        var awaitableEvent = eventField.GetValue( scheduler )!;
        var aeType = awaitableEvent.GetType();
        var signalField = aeType.GetField( "SignalState", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance )!;
        var opsField = aeType.GetField( "Operations", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance )!;

        for ( var i = 0; i < 2_000_000; i++ )
        {
            scheduler.EnqueueBackgroundTask( _ => Task.CompletedTask );

            var completed = scheduler.WhenBackgroundTasksCompleted( CancellationToken.None );

            if ( await Task.WhenAny( completed, Task.Delay( 5000 ) ) != completed )
            {
                // Disambiguate a genuine lost wakeup (op stuck in WAITING/CREATED, never activated) from
                // thread-pool starvation (op already SUCCESS, continuation just queued behind the loop's own
                // work). Read the queued op's State and then wait much longer to see whether it resolves.
                string DumpOpStates()
                {
                    var ops = (IEnumerable) opsField.GetValue( awaitableEvent )!;
                    var states = new List<string>();

                    foreach ( var op in ops )
                    {
                        var st = (int) op.GetType().GetField( "State", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance )!
                            .GetValue( op )!;

                        states.Add(
                            st switch
                            {
                                0 => "CREATED",
                                1 => "WAITING",
                                2 => "SUCCESS",
                                3 => "TIMEOUT",
                                _ => st.ToString( CultureInfo.InvariantCulture )
                            } );
                    }

                    return string.Join( ",", states );
                }

                this._output.WriteLine(
                    $"[at 5s] iteration={i} taskCount={countField.GetValue( scheduler )} signalState={signalField.GetValue( awaitableEvent )} "
                    + $"opStates=[{DumpOpStates()}] completed.IsCompleted={completed.IsCompleted}" );

                // Wait up to a further 60s, checking whether it eventually completes (=> starvation, not lost wakeup).
                var extra = 0;

                while ( !completed.IsCompleted && extra < 60000 )
                {
                    await Task.Delay( 1000 );
                    extra += 1000;
                }

                this._output.WriteLine(
                    $"[after +{extra}ms] iteration={i} taskCount={countField.GetValue( scheduler )} signalState={signalField.GetValue( awaitableEvent )} "
                    + $"opStates=[{DumpOpStates()}] completed.IsCompleted={completed.IsCompleted}" );

                Assert.Fail(
                    completed.IsCompleted
                        ? $"STARVATION at iteration {i}: completed after an extra {extra}ms (not a lost wakeup)."
                        : $"LOST WAKEUP at iteration {i}: still not completed after {5000 + extra}ms." );
            }
        }

        await scheduler.DisposeAsync();
    }
}
