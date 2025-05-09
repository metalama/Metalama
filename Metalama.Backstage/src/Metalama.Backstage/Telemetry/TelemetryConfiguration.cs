// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Backstage.Telemetry;

[ConfigurationFile( "telemetry.json" )]
public sealed record TelemetryConfiguration : ConfigurationFile
{
    public ReportingAction ExceptionReportingAction { get; init; } = ReportingAction.Default;

    public ReportingAction PerformanceProblemReportingAction { get; init; } = ReportingAction.Default;

    public ReportingAction UsageReportingAction { get; init; } = ReportingAction.Default;

    public ReportingAction GetReportingAction( TelemetryScenario scenario )
        => scenario switch
        {
            TelemetryScenario.Exception => this.ExceptionReportingAction,
            TelemetryScenario.Performance => this.PerformanceProblemReportingAction,
            TelemetryScenario.Usage => this.UsageReportingAction,
            _ => throw new ArgumentOutOfRangeException()
        };

    // Do not consume directly this property, as it may not be initialized. Consume it through ITelemetryConfigurationService.
    public Guid? DeviceId { get; init; }

    public DateTime? LastUploadTime { get; init; }

    /// <summary>
    /// Gets the value with PIIs should be salted.
    /// </summary>
    public long? Salt { get; init; }

    /// <summary>
    /// Gets the last time the <see cref="Salt"/> and <see cref="DeviceId"/> properties was rotated. This should be done monthly.
    /// </summary>
    public DateTime? LastSaltChangeTime { get; init; }

    public ImmutableDictionary<string, ReportingStatus> Issues { get; init; } =
        ImmutableDictionary<string, ReportingStatus>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase );

    public ImmutableDictionary<string, DateTime> Sessions { get; init; } =
        ImmutableDictionary<string, DateTime>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase );

    public DateTime? LastMatomoPostTime { get; init; }

    public TelemetryConfiguration CleanUp( DateTime threshold )
    {
        return this with
        {
            Sessions = this.Sessions.Where( s => s.Value.Date >= threshold ).ToImmutableDictionary( k => k.Key, k => k.Value, this.Sessions.KeyComparer )
        };
    }
}