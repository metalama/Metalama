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
    /// <summary>
    /// The name of the environment variable that opts the whole machine out of telemetry, regardless of the per-category
    /// configuration. A non-empty value other than <c>false</c>/<c>0</c> disables telemetry at the process level.
    /// </summary>
    public const string OptOutEnvironmentVariableName = "METALAMA_TELEMETRY_OPT_OUT";

    [JsonPropertyName( "ExceptionReportingAction" )]
    public TelemetryConsent ExceptionConsent { get; init; } = TelemetryConsent.Default;

    [JsonPropertyName( "PerformanceProblemReportingAction" )]
    public TelemetryConsent PerformanceProblemConsent { get; init; } = TelemetryConsent.Default;

    [JsonPropertyName( "UsageReportingAction" )]
    public TelemetryConsent UsageConsent { get; init; } = TelemetryConsent.Default;

    public TelemetryConsent GetConsent( TelemetryScenario scenario )
        => scenario switch
        {
            TelemetryScenario.Exception => this.ExceptionConsent,
            TelemetryScenario.Performance => this.PerformanceProblemConsent,
            TelemetryScenario.Usage => this.UsageConsent,
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

    public long? LicenseAuditSalt { get; init; }

    /// <summary>
    /// Gets the last time the <see cref="MatomoSalt"/>, <see cref="UsageTrackingSalt"/>, <see cref="ExceptionReportingSalt"/>
    /// and <see cref="DeviceId"/> properties were rotated. This should be done monthly.
    /// </summary>
    public DateTime? LastSaltChangeTime { get; init; }

    /// <summary>
    /// Gets the terminal decision taken for an issue, keyed by its invariant hash: <see cref="ReportingStatus.Reported"/>
    /// once the report has actually been sent, and <see cref="ReportingStatus.Ignored"/> once the user has asked never to
    /// report that issue. An issue on which no decision has been taken yet is absent.
    /// </summary>
    /// <remarks>
    /// Only decisions are recorded here. Merely capturing a report does not add an entry, otherwise an issue the user
    /// never approved would be silenced forever. The state of the pending question lives in <see cref="IssuePrompts"/>.
    /// See #1751.
    /// </remarks>
    [JsonConverter( typeof(CaseInsensitiveImmutableDictionaryConverterFactory<ReportingStatus>) )]
    public ImmutableDictionary<string, ReportingStatus> Issues
    {
        get => this._issues;
        init => this._issues = value ?? _emptyIssues;
    }

    /// <summary>
    /// Gets the UTC time at which the user was last prompted to review the report of an issue, keyed by its invariant
    /// hash. An issue on which no decision has been taken yet is prompted again once the retry period has elapsed.
    /// </summary>
    /// <remarks>
    /// The review notification is the only way to approve a report, so a notification the user missed or dismissed must
    /// not silence the issue forever: the question is asked again the next time the issue occurs. Entries older than the
    /// retry period are pruned as they are written, so this dictionary only ever holds the very recently prompted
    /// issues. See #1751.
    /// </remarks>
    [JsonConverter( typeof(CaseInsensitiveImmutableDictionaryConverterFactory<DateTime>) )]
    public ImmutableDictionary<string, DateTime> IssuePrompts
    {
        get => this._issuePrompts;
        init => this._issuePrompts = value ?? _emptyDates;
    }

    [JsonConverter( typeof(CaseInsensitiveImmutableDictionaryConverterFactory<DateTime>) )]
    public ImmutableDictionary<string, DateTime> Sessions
    {
        get => this._sessions;
        init => this._sessions = value ?? _emptyDates;
    }

    // A property that is absent from the JSON file deserializes to null rather than to its initializer, so every
    // dictionary normalizes null in its 'init' accessor (which 'with' expressions also go through). Without this, a
    // configuration file written before one of these properties existed - or one the user edited through
    // 'metalama config edit telemetry' - makes the consumers throw a NullReferenceException. See #1751.
    private static readonly ImmutableDictionary<string, ReportingStatus> _emptyIssues =
        ImmutableDictionary<string, ReportingStatus>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase );

    private static readonly ImmutableDictionary<string, DateTime> _emptyDates =
        ImmutableDictionary<string, DateTime>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase );

    private readonly ImmutableDictionary<string, ReportingStatus> _issues = _emptyIssues;
    private readonly ImmutableDictionary<string, DateTime> _issuePrompts = _emptyDates;
    private readonly ImmutableDictionary<string, DateTime> _sessions = _emptyDates;

    public DateTime? LastMatomoPostTime { get; init; }

    /// <summary>
    /// The retention period applied when <see cref="RetentionPeriodInDays"/> is not set.
    /// </summary>
    public const int DefaultRetentionPeriodInDays = 30;

    /// <summary>
    /// Gets the number of days during which telemetry data is retained on disk before being deleted by the
    /// maintenance pass. The period is read live at each cleanup, so changing it takes effect on the next sweep.
    /// <c>null</c> (the default for a new configuration, and for a configuration that omits the setting) means
    /// <see cref="DefaultRetentionPeriodInDays"/> (30 days), applied at cleanup time.
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