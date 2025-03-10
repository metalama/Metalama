// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using System;
#if DEBUG
using System.Diagnostics;
#endif
using System.Threading;

namespace Metalama.Backstage.Utilities;

internal sealed class MutexHandle : IDisposable
{
    private readonly Mutex _mutex;
    private readonly string _name;
    private readonly ILogger? _logger;
    private readonly object _disposingSync = new object();

#if DEBUG
    private readonly StackTrace _stackTrace = new();
#endif
    
    private bool _disposed;
    
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
        lock ( this._disposingSync )
        {
            if ( !this._disposed )
            {
                this._mutex.ReleaseMutex();
                this._mutex.Dispose();
                this._disposed = true;
            }
        }
    }

    ~MutexHandle()
    {
        this.Dispose( false );
        
#if DEBUG
        throw new InvalidOperationException( "The mutex was not disposed. It was acquired here: " + Environment.NewLine + this._stackTrace );
#endif
    }
}