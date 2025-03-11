// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Infrastructure;

internal sealed class ShutdownService : IBackstageService
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    
    public ShutdownService( IServiceProvider serviceProvider )
    {
        this._loggerFactory = serviceProvider.GetLoggerFactory();
        this._logger = this._loggerFactory.GetLogger( nameof(ShutdownService) );
    }
    
    public void Initialize()
    {
        this._logger.Trace?.Log( "Registering the shutdown service." );
        AppDomain.CurrentDomain.ProcessExit += this.OnProcessExit;
    }
    
    private void OnProcessExit( object? sender, EventArgs e )
    {
        this._logger.Trace?.Log( "The process is shutting down." );
        
        this._logger.Trace?.Log( "Completing background tasks." );
        BackstageBackgroundTasksService.Default.CompleteAsync().Wait();
        
        this._logger.Trace?.Log( "Flushing logs." );
        this._loggerFactory.Flush();
        
        // We don't trace any further, as further logs might not be written without another explicit flush.
    }
}