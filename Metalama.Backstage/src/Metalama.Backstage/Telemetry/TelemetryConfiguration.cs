// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Serialization;
using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Telemetry;

[ConfigurationFile( "telemetry.json" )]
[Description( "Telemetry and exception-reporting options." )]
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
    /// Gets the value with which PIIs sent to the third-party analytics platform (Matomo) should be salted.
    /// The persisted JSON key is intentionally kept as <c>Salt</c> to preserve Matomo visitor continuity. See issue #1668.
    /// </summary>
    [JsonPropertyName( "Salt" )]
    public long? MatomoSalt { get; init; }

    /// <summary>
    /// Gets the value with which identifiers sent only to the first-party diagnostic store (bits) by the
    /// usage-tracking channel (the license-audit report) should be salted. This is distinct from
    /// <see cref="MatomoSalt"/> and <see cref="ExceptionReportingSalt"/> so that the usage-tracking pseudonym
    /// cannot be correlated with the Matomo dataset nor with the exception-reporting data. See issue #1668.
    /// </summary>
    public long? UsageTrackingSalt { get; init; }

    /// <summary>
    /// Gets the value with which identifiers sent only to the first-party diagnostic store (bits) by the
    /// exception-reporting channel should be salted. This is distinct from <see cref="MatomoSalt"/> and
    /// <see cref="UsageTrackingSalt"/> so that the exception-reporting pseudonym cannot be correlated with the
    /// Matomo dataset nor with the usage-tracking data. See issue #1668.
    /// </summary>
    public long? ExceptionReportingSalt { get; init; }

    /// <summary>
    /// Gets the last time the <see cref="MatomoSalt"/>, <see cref="UsageTrackingSalt"/>, <see cref="ExceptionReportingSalt"/>
    /// and <see cref="DeviceId"/> properties were rotated. This should be done monthly.
    /// </summary>
    public DateTime? LastSaltChangeTime { get; init; }

    [JsonConverter( typeof(CaseInsensitiveImmutableDictionaryConverterFactory<ReportingStatus>) )]
    public ImmutableDictionary<string, ReportingStatus> Issues { get; init; } =
        ImmutableDictionary<string, ReportingStatus>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase );

    [JsonConverter( typeof(CaseInsensitiveImmutableDictionaryConverterFactory<DateTime>) )]
    public ImmutableDictionary<string, DateTime> Sessions { get; init; } =
        ImmutableDictionary<string, DateTime>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase );

    public DateTime? LastMatomoPostTime { get; init; }

    /// <summary>
    /// Gets the number of days during which telemetry data is retained on disk before being deleted by the
    /// maintenance pass. The period is read live at each cleanup, so changing it takes effect on the next sweep.
    /// The default value is 30 days.
    /// </summary>
    public int? RetentionPeriodInDays { get; init; }

    public TelemetryConfiguration CleanUp( DateTime threshold )
    {
        return this with
        {
            Sessions = this.Sessions.Where( s => s.Value.Date >= threshold ).ToImmutableDictionary( k => k.Key, k => k.Value, this.Sessions.KeyComparer )
        };
    }
}