// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Telemetry;

internal sealed class UsageSessionFactory : IUsageSessionFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;

    public object Sync { get; } = new();

    public UsageSessionFactory( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
    }

    public IUsageSession CreateSession( string kind, bool shouldCollectMetrics = true )
    {
        // We are about to report usage, so make sure telemetry is activated (the DeviceId and salts exist). Activation
        // is lazy so that a process which never reports also never creates a device identifier.
        // We don't check if telemetry is enabled because this layer is behind the consent layer.
        this._telemetryConfigurationService.EnsureActivated();

        return new UsageSession( this._serviceProvider, kind, shouldCollectMetrics );
    }
}