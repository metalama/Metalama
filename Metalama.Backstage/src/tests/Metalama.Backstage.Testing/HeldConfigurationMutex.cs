// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Utilities;
using System;
using System.Threading;

namespace Metalama.Backstage.Testing;

/// <summary>
/// Holds the global configuration mutex of a test service provider until it is disposed, so that a test can observe how
/// the production code behaves when the mutex cannot be acquired. See issue #1847.
/// </summary>
/// <remarks>
/// <para>
/// The mutex is held on a dedicated thread, because a mutex is owned by a thread and its owning thread can always
/// acquire it again. Holding it on the thread that runs the test would therefore have no effect on the code under test.
/// </para>
/// <para>
/// The mutex is named after the same inputs as the one of <c>ConfigurationManager</c>, and
/// <see cref="TestFileSystem.SynchronizationPrefix"/> is unique to each test, so no two tests contend for the same
/// mutex.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class HeldConfigurationMutex : IDisposable
{
    /// <summary>
    /// The time during which the constructor waits for the mutex. It is a safety net against a test hanging forever,
    /// not an expected duration: the mutex is normally acquired immediately.
    /// </summary>
    private static readonly TimeSpan _acquisitionTimeout = TimeSpan.FromSeconds( 30 );

    private readonly Mutex _mutex;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _acquiredEvent = new();
    private readonly ManualResetEventSlim _releaseEvent = new();

    public HeldConfigurationMutex( IServiceProvider serviceProvider )
    {
        var fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        var standardDirectories = serviceProvider.GetRequiredBackstageService<IStandardDirectories>();

        this._mutex = MutexHelper.OpenOrCreateMutex( standardDirectories.ApplicationDataDirectory, fileSystem.SynchronizationPrefix, null );

        this._thread = new Thread( this.HoldMutex ) { IsBackground = true, Name = nameof(HeldConfigurationMutex) };
        this._thread.Start();

        if ( !this._acquiredEvent.Wait( _acquisitionTimeout ) )
        {
            throw new TimeoutException( $"The configuration mutex could not be acquired in {_acquisitionTimeout}." );
        }
    }

    private void HoldMutex()
    {
        this._mutex.WaitOne();

        try
        {
            this._acquiredEvent.Set();
            this._releaseEvent.Wait();
        }
        finally
        {
            this._mutex.ReleaseMutex();
        }
    }

    public void Dispose()
    {
        this._releaseEvent.Set();
        this._thread.Join();

        this._mutex.Dispose();
        this._acquiredEvent.Dispose();
        this._releaseEvent.Dispose();
    }
}
