// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Metalama.Framework.Engine.Utilities;

/// <summary>
/// Collects the standard output and the standard error of a child process, one item per line, and waits until the
/// process has terminated and both streams have been read to their end.
/// </summary>
/// <remarks>
/// <para>
/// The two streams are read on different threads, so access to the collected lines is synchronized.
/// </para>
/// <para>
/// <see cref="Process.WaitForExit(int)"/> does not wait until the asynchronous readers have processed the end of the
/// redirected streams, so lines that the process wrote before it exited can still be pending when it returns. Each
/// stream signals its end with a <see cref="DataReceivedEventArgs.Data"/> value of <c>null</c>, and
/// <see cref="WaitForCompletion"/> waits for both signals. The wait is bounded, because a descendant process that
/// inherited the handles of the streams can keep them open after the process itself has terminated.
/// </para>
/// <para>
/// The class uses a monitor rather than a disposable wait handle, because an end-of-stream signal can arrive after the
/// bounded wait has expired, and it must then not fail on a disposed object.
/// </para>
/// </remarks>
internal sealed class ProcessOutputCollector
{
    /// <summary>
    /// The maximum time, in milliseconds, that <see cref="WaitForCompletion"/> waits for the process to terminate and,
    /// separately, for its output streams to be read to their end.
    /// </summary>
    private const int _completionTimeout = 5_000;

    private readonly object _sync = new();
    private readonly List<string> _lines = new();
    private int _openStreamCount = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessOutputCollector"/> class and subscribes to the output events
    /// of <paramref name="process"/>.
    /// </summary>
    /// <remarks>
    /// The instance must be created before the asynchronous reading of the streams is started with
    /// <see cref="Process.BeginOutputReadLine"/> and <see cref="Process.BeginErrorReadLine"/>.
    /// </remarks>
    public ProcessOutputCollector( Process process )
    {
        process.OutputDataReceived += this.OnDataReceived;
        process.ErrorDataReceived += this.OnDataReceived;
    }

    private void OnDataReceived( object sender, DataReceivedEventArgs e )
    {
        lock ( this._sync )
        {
            if ( e.Data == null )
            {
                this._openStreamCount--;
                Monitor.PulseAll( this._sync );
            }
            else
            {
                this._lines.Add( e.Data );
            }
        }
    }

    /// <summary>
    /// Waits, for a bounded time, until <paramref name="process"/> has terminated and both of its output streams have
    /// been read to their end.
    /// </summary>
    /// <remarks>
    /// The method returns when the bound expires even if the process is still running or a stream is still open. The
    /// lines collected until then remain available through <see cref="GetLines"/>.
    /// </remarks>
    public void WaitForCompletion( Process process )
    {
        process.WaitForExit( _completionTimeout );

        var stopwatch = Stopwatch.StartNew();

        lock ( this._sync )
        {
            while ( this._openStreamCount > 0 )
            {
                var remaining = _completionTimeout - (int) stopwatch.ElapsedMilliseconds;

                if ( remaining <= 0 || !Monitor.Wait( this._sync, remaining ) )
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Gets the lines collected so far from the standard output and the standard error.
    /// </summary>
    public ImmutableArray<string> GetLines()
    {
        lock ( this._sync )
        {
            return this._lines.ToImmutableArray();
        }
    }
}
