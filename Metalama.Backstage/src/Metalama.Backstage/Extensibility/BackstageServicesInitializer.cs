// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
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
    private readonly IRssClient? _rssClient;

    public BackstageServicesInitializer( IServiceProvider serviceProvider, BackstageInitializationOptions options )
    {
        this._serviceProvider = serviceProvider;
        this._options = options;
        this._backgroundTasksService = serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>();
        this._profilingService = serviceProvider.GetBackstageService<IProfilingService>();
        this._shutdownService = serviceProvider.GetBackstageService<ShutdownService>();
        this._telemetryConfigurationService = serviceProvider.GetBackstageService<ITelemetryConfigurationService>();

        if ( options.NotifyOfLatestNews )
        {
            this._rssClient = serviceProvider.GetBackstageService<IRssClient>();
        }
    }

    public void Initialize()
    {
        this._profilingService?.Initialize();
        this._telemetryConfigurationService?.Initialize();
        this._shutdownService?.Initialize();
        this._rssClient?.Initialize();

        // The license manager may enqueue a file but be unable to start the process.
        var telemetryUploader = this._serviceProvider.GetBackstageService<ITelemetryUploader>();

        if ( telemetryUploader != null && this._options.AutoUploadTelemetry )
        {
            this._backgroundTasksService.Enqueue( () => telemetryUploader.StartUpload() );
        }
    }
}