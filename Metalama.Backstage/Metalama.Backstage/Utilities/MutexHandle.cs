// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using System;
using System.Diagnostics;
using System.Threading;

namespace Metalama.Backstage.Utilities;

internal sealed class MutexHandle : IDisposable
{
    private readonly Mutex _mutex;
    private readonly string _name;
    private readonly ILogger? _logger;

#if DEBUG
    private readonly StackTrace _stackTrace = new();
#endif

    public MutexHandle( Mutex mutex, string name, ILogger? logger )
    {
        this._mutex = mutex;
        this._name = name;
        this._logger = logger;
    }

    public void Dispose()
    {
        this._logger?.Trace?.Log( $"Releasing lock '{this._name}'." );

        this.Dispose( true );

        GC.SuppressFinalize( this );
    }

    private void Dispose( bool disposing )
    {
        this._mutex.ReleaseMutex();
        this._mutex.Dispose();
    }

#pragma warning disable CA1821
#if DEBUG
    ~MutexHandle()
    {
        this.Dispose( false );

        throw new InvalidOperationException( "The mutex was not disposed. It was acquired here: " + Environment.NewLine + this._stackTrace );
    }
#endif
#pragma warning restore CA1821
}