// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
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

        // Gets the first-use date.
        var applicationDataDirectory = serviceProvider.GetRequiredBackstageService<IStandardDirectories>().ApplicationDataDirectory;
        var fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        var firstUseDate = fileSystem.GetDirectoryCreationTime( applicationDataDirectory );
        var today = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>().UtcNow;

        this.DeviceAgeBucket =
            (today - firstUseDate).TotalDays switch
            {
                < 1 => DeviceAgeBucket.LessThan1,
                <= 30 => DeviceAgeBucket.From1To30,
                _ => DeviceAgeBucket.MoreThan30
            };
    }

    public DeviceAgeBucket DeviceAgeBucket { get; }

    public Version? AssemblyVersion => this.ReportedComponent.AssemblyVersion;

    protected string ApplicationName => this.ReportedComponent.Name;

#pragma warning disable CA1822

    // The user hash is only ever sent to the first-party diagnostic store (bits), never to Matomo, so it is
    // keyed by DiagnosticSalt to keep it unjoinable to the Matomo dataset. See #1668.
    public long UserHash => HashUtilities.ComputeInt64Hmac( Environment.UserName, this._telemetryConfigurationService.InternalDiagnosticSalt );
#pragma warning restore CA1822

    // The device hash sent to the third-party analytics platform (Matomo). Keyed by Salt.
    // DeviceId is already rotated monthly, so there is no need to salt it further.
    public long DeviceHash
        => HashUtilities.ComputeInt64Hmac( this._telemetryConfigurationService.DeviceId.ToString(), this._telemetryConfigurationService.Salt );

    // The device hash sent only to the first-party diagnostic store (bits). Keyed by DiagnosticSalt so that it
    // cannot be correlated with the Matomo DeviceHash. See #1668.
    public long InternalDeviceHash
        => HashUtilities.ComputeInt64Hmac( this._telemetryConfigurationService.DeviceId.ToString(), this._telemetryConfigurationService.InternalDiagnosticSalt );
}