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

    protected abstract TelemetrySaltKind DetailedTrackingHashKind { get; }

    // We use the same salt for all reports to Matomo because one of the two Matomo data channels carry any
    // sensitive information, so correlating between both does not matter.
    private const TelemetrySaltKind _aggregateHashKind = TelemetrySaltKind.Matomo;

#pragma warning disable CA1822

    /// <summary>
    /// Gets the identifier of the current user reported by the license audit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value is computed by <see cref="HashUtilities.ComputeStringHash64"/>, which is the algorithm PostSharp
    /// uses. It is unkeyed, so the same account name gives the same value on every machine and in both products.
    /// The license audit therefore counts one person once, whatever the number of machines that person uses and
    /// whatever the mixture of the two products. See issue #1873.
    /// </para>
    /// <para>
    /// This value is only ever sent to the first-party store, by the license audit report, and never to Matomo. The
    /// Matomo channel and the exception reporting channel keep their salted and monthly rotated identifiers, which
    /// stay unjoinable to this one. See issue #1668.
    /// </para>
    /// </remarks>
    public long CrossProductUserHash => HashUtilities.ComputeStringHash64( Environment.UserName );
#pragma warning restore CA1822

    // The device hash sent to the third-party analytics platform (Matomo). Keyed by MatomoSalt.
    // DeviceId is already rotated monthly, so there is no need to salt it further.
    public long AggregateTrackingDeviceHash
        => HashUtilities.ComputeInt64Hmac(
            this._telemetryConfigurationService.DeviceId.ToString(),
            this._telemetryConfigurationService.GetSalt( _aggregateHashKind ) );

    // The device hash sent only to the first-party diagnostic store (bits) by the usage-tracking channel (the
    // license-audit report). Keyed by UsageTrackingSalt so that it cannot be correlated with the Matomo dataset
    // (the MatomoDeviceHash) nor with the exception-reporting device hash. See #1668.
    public long DetailedTrackingDeviceHash
        => HashUtilities.ComputeInt64Hmac(
            this._telemetryConfigurationService.DeviceId.ToString(),
            this._telemetryConfigurationService.GetSalt( this.DetailedTrackingHashKind ) );
}