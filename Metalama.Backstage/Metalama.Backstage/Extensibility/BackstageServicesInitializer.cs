// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
using System;

namespace Metalama.Backstage.Extensibility;

internal sealed class BackstageServicesInitializer : IBackstageService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;

    public BackstageServicesInitializer( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._backgroundTasksService = serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>();
    }

    public void Initialize()
    {
        this._serviceProvider.GetBackstageService<IProfilingService>()?.Initialize();

        // The license manager may enqueue a file but be unable to start the process.
        this._backgroundTasksService.Enqueue( () => this._serviceProvider.GetBackstageService<ITelemetryUploader>()?.StartUpload() );
    }
}