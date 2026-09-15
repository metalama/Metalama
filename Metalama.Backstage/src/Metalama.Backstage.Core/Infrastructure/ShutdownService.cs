// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Infrastructure;

/// <summary>
/// Drains the background task queue and flushes the logs when the process exits. There is one instance per service
/// provider, and each instance drains the queue of its own provider.
/// </summary>
internal sealed class ShutdownService : IBackstageService, IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;
    private bool _isInitialized;

    public ShutdownService( IServiceProvider serviceProvider )
    {
        this._loggerFactory = serviceProvider.GetLoggerFactory();
        this._logger = this._loggerFactory.GetLogger( nameof(ShutdownService) );
        this._backgroundTasksService = serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>();
    }

    /// <summary>
    /// Subscribes to the exit of the process. The method is idempotent, because it is invoked both by the services
    /// initializer and by the host.
    /// </summary>
    public void Initialize()
    {
        if ( this._isInitialized )
        {
            return;
        }

        this._isInitialized = true;
        this._logger.Trace?.Log( "Registering the shutdown service." );
        AppDomain.CurrentDomain.ProcessExit += this.OnProcessExit;
    }

    /// <summary>
    /// Unsubscribes from the exit of the process, so that a disposed provider no longer drains its queue at that time.
    /// </summary>
    public void Dispose()
    {
        if ( this._isInitialized )
        {
            this._isInitialized = false;
            AppDomain.CurrentDomain.ProcessExit -= this.OnProcessExit;
        }
    }

    private void OnProcessExit( object? sender, EventArgs e )
    {
        this._logger.Trace?.Log( "The process is shutting down." );

        this._logger.Trace?.Log( "Completing background tasks." );
        this._backgroundTasksService.CompleteAsync().Wait();

        this._logger.Trace?.Log( "Flushing logs." );
        this._loggerFactory.Flush();

        // We don't trace any further, as further logs might not be written without another explicit flush.
    }
}