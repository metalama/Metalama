// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Welcome;
using System;

namespace Metalama.Backstage.Extensibility;

internal sealed class BackstageServicesInitializer : IBackstageService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;
    private readonly WelcomeService? _welcomeService;
    private readonly IProfilingService? _profilingService;

    public BackstageServicesInitializer( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._backgroundTasksService = serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>();
        this._profilingService = serviceProvider.GetBackstageService<IProfilingService>();
        this._welcomeService = serviceProvider.GetBackstageService<WelcomeService>();
    }

    public void Initialize()
    {
        this._profilingService?.Initialize();
        this._welcomeService?.OnBackstageInitialized();

        // The license manager may enqueue a file but be unable to start the process.
        var telemetryUploader = this._serviceProvider.GetBackstageService<ITelemetryUploader>();

        if ( telemetryUploader != null )
        {
            this._backgroundTasksService.Enqueue( () => telemetryUploader.StartUpload() );
        }
    }
}