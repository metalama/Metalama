// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Runs an action on a dedicated, named background thread and captures the exception that the action throws.
/// </summary>
/// <remarks>
/// An exception that escapes a raw <see cref="Thread"/> terminates the test host. This class catches it instead, so
/// that the test can report it. A worker that stays blocked, for example on a monitor held by a deadlocked thread, does
/// not prevent the test host from exiting, because the thread is a background thread.
/// </remarks>
internal sealed class ConcurrencyTestWorker
{
    private readonly Thread _thread;
    private Exception? _exception;

    private ConcurrencyTestWorker( string name, Action action )
    {
        this.Name = name;

        this._thread = new Thread(
            () =>
            {
                try
                {
                    action();
                }
                catch ( Exception e )
                {
                    Volatile.Write( ref this._exception, e );
                }
            } ) { IsBackground = true, Name = name };
    }

    /// <summary>
    /// Starts a worker.
    /// </summary>
    /// <param name="name">The name of the thread. Gates select a thread by this name.</param>
    /// <param name="action">The action to run.</param>
    /// <returns>The started worker.</returns>
    public static ConcurrencyTestWorker Start( string name, Action action )
    {
        var worker = new ConcurrencyTestWorker( name, action );
        worker._thread.Start();

        return worker;
    }

    /// <summary>
    /// Gets the name of the thread.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the exception that the action threw, or <see langword="null"/> when it threw none or has not completed.
    /// </summary>
    public Exception? Exception => Volatile.Read( ref this._exception );

    /// <summary>
    /// Waits until the action completes. The timeout only detects a failure, such as a deadlock.
    /// </summary>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <returns><see langword="true"/> when the action has completed, otherwise <see langword="false"/>.</returns>
    public bool Join( TimeSpan timeout ) => this._thread.Join( timeout );
}
