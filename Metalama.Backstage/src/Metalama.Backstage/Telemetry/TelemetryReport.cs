// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Utilities;
using System;

namespace Metalama.Backstage.Telemetry;

internal abstract class TelemetryReport
{
    public IComponentInfo ReportedComponent { get; }

    private readonly ITelemetryConfigurationService _telemetryConfigurationService;

    public abstract string Kind { get; }

    public MetricCollection Metrics { get; }

    protected TelemetryReport( IServiceProvider serviceProvider, MetricCollection metrics )
    {
        this.Metrics = metrics;
        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();

        // Note that we are intentionally and "randomly" reporting the version of the first component that
        // triggered audit, to prioritize having just one hit per day over having accurate version reporting
        // (at least for Matomo reporting).
        this.ReportedComponent = serviceProvider
            .GetRequiredBackstageService<IApplicationInfoProvider>()
            .CurrentApplication
            .GetLatestComponentMadeByPostSharp();
    }

    public Version? AssemblyVersion => this.ReportedComponent.AssemblyVersion;

    public string ApplicationName => this.ReportedComponent.Name;

#pragma warning disable CA1822
    public long UserHash => HashUtilities.ComputeInt64Hmac( Environment.UserName, this._telemetryConfigurationService.Salt );
#pragma warning restore CA1822

    // DeviceId is already rotated monthly, so there is no need to salt it.
    public long DeviceHash => HashUtilities.ComputeInt64Hmac( this._telemetryConfigurationService.DeviceId.ToString(), this._telemetryConfigurationService.Salt );
}