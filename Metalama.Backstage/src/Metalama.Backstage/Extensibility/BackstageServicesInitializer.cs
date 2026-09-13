// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.UserInterface.Rss;
using System;

namespace Metalama.Backstage.Extensibility;

internal sealed class BackstageServicesInitializer : IBackstageService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BackstageInitializationOptions _options;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;
    private readonly IProfilingService? _profilingService;
    private readonly ITelemetryConfigurationService? _telemetryConfigurationService;
    private readonly ShutdownService? _shutdownService;
    private bool _isInitialized;

    public BackstageServicesInitializer( IServiceProvider serviceProvider, BackstageInitializationOptions options )
    {
        this._serviceProvider = serviceProvider;
        this._options = options;
        this._backgroundTasksService = serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>();
        this._profilingService = serviceProvider.GetBackstageService<IProfilingService>();
        this._shutdownService = serviceProvider.GetBackstageService<ShutdownService>();
        this._telemetryConfigurationService = serviceProvider.GetBackstageService<ITelemetryConfigurationService>();
    }

    /// <summary>
    /// Initializes the services that need it, in the order of their dependencies. It is called once, after the
    /// service provider is built.
    /// </summary>
    /// <exception cref="InvalidOperationException">The method has already been called.</exception>
    public void Initialize()
    {
        if ( this._isInitialized )
        {
            throw new InvalidOperationException( "The Backstage services have already been initialized." );
        }

        this._isInitialized = true;

        // Before anything is enqueued, so that a background task that fails is reported instead of vanishing. See #1765.
        this._backgroundTasksService.SetLogger( this._serviceProvider.GetLoggerFactory().GetLogger( "BackgroundTasks" ) );

        this._profilingService?.Initialize();
        this._telemetryConfigurationService?.Initialize();
        this._shutdownService?.Initialize();

        // The subscribers of the event dispatcher subscribe when they are initialized, because a service that nobody
        // resolves is never created and would therefore never subscribe by itself.
        this._serviceProvider.GetBackstageService<UserInterfaceEventSubscriber>()?.Initialize();
        (this._serviceProvider.GetBackstageService<IRssClient>() as RssClient)?.Initialize();

        // The license manager may enqueue a file but be unable to start the process.
        var telemetryUploader = this._serviceProvider.GetBackstageService<ITelemetryUploader>();

        // Do not attempt the auto-upload when telemetry has never been activated. Starting an upload advances
        // LastUploadTime (and would therefore create telemetry.json) even when nothing has been captured, which would
        // violate the lazy-activation guarantee that a process which never reports leaves the global configuration
        // untouched. A not-yet-activated configuration also has nothing queued to upload. See #1701.
        if ( telemetryUploader != null
             && this._options.AutoUploadTelemetry
             && this._telemetryConfigurationService?.IsActivated != false )
        {
            this._backgroundTasksService.Enqueue( () => telemetryUploader.StartUpload() );
        }
    }
}