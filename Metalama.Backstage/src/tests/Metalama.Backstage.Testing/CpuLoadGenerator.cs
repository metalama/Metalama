// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Metalama.Backstage.Testing;

/// <summary>
/// Saturates the processors of the machine for as long as it is not disposed, so that a load test runs against a
/// scheduler that actually preempts its threads.
/// </summary>
/// <remarks>
/// <para>
/// A race whose window is a few instructions wide is very unlikely to be observed on an idle machine, because a
/// thread that is never preempted runs its critical section to completion. Saturating the processors makes
/// preemption ordinary, which is what turns a load test from a throughput measurement into a search for a defect.
/// </para>
/// <para>
/// The threads run at <see cref="ThreadPriority.BelowNormal"/>. At normal priority they would compete with the code
/// under test rather than merely interleave with it, which measures the scheduler instead of the code, and would
/// make the machine unusable for as long as the test runs.
/// </para>
/// <para>
/// Set the <c>METALAMA_TESTS_NO_CPU_LOAD</c> environment variable to make this class do nothing, which is what a
/// developer who wants to keep using the machine does.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class CpuLoadGenerator : IDisposable
{
    /// <summary>
    /// The name of the environment variable that disables this class.
    /// </summary>
    public const string DisableEnvironmentVariableName = "METALAMA_TESTS_NO_CPU_LOAD";

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly List<Thread> _threads = new();
    private int _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CpuLoadGenerator"/> class and starts the load.
    /// </summary>
    /// <param name="log">An optional delegate receiving trace messages, typically the <c>WriteLine</c> method of a test output helper.</param>
    /// <remarks>
    /// Instantiate this inside the test method rather than in a fixture, so that a load test accidentally left
    /// un-skipped saturates the machine for the duration of one test and not of the whole run.
    /// </remarks>
    public CpuLoadGenerator( Action<string>? log = null )
    {
        if ( !string.IsNullOrEmpty( Environment.GetEnvironmentVariable( DisableEnvironmentVariableName ) ) )
        {
            log?.Invoke( $"CpuLoadGenerator: disabled by {DisableEnvironmentVariableName}." );

            return;
        }

        // One thread fewer than there are processors, so that the operating system keeps somewhere to run.
        var threadCount = Math.Max( 1, Environment.ProcessorCount - 1 );

        log?.Invoke( $"CpuLoadGenerator: starting {threadCount} thread(s)." );

        for ( var i = 0; i < threadCount; i++ )
        {
            var thread = new Thread( this.Burn ) { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = $"CpuLoadGenerator-{i}" };

            this._threads.Add( thread );
            thread.Start();
        }
    }

    /// <summary>
    /// Consumes processor time until the load is cancelled.
    /// </summary>
    private void Burn()
    {
        var cancellationToken = this._cancellationTokenSource.Token;
        var accumulator = 0d;

        while ( !cancellationToken.IsCancellationRequested )
        {
            // Arithmetic rather than a spin-wait primitive, because a spin-wait yields, and a thread that yields
            // does not compete for the processor.
            for ( var i = 1; i < 10_000; i++ )
            {
                accumulator += Math.Sqrt( i );
            }

            // Reset rather than accumulate, so that the value cannot grow to infinity and let the runtime elide the
            // loop as constant.
            if ( accumulator > 1e12 )
            {
                accumulator = 0;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if ( Interlocked.Exchange( ref this._isDisposed, 1 ) != 0 )
        {
            return;
        }

        this._cancellationTokenSource.Cancel();

        foreach ( var thread in this._threads )
        {
            thread.Join();
        }

        this._cancellationTokenSource.Dispose();
    }
}
