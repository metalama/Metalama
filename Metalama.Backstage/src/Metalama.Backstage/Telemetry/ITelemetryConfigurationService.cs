// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Telemetry;

[PublicAPI]
public interface ITelemetryConfigurationService : IBackstageService
{
    /// <summary>
    /// Initializes the service: computes the process-level enablement (see <see cref="IsGloballyEnabled"/>) and reads
    /// the persisted configuration. It does NOT activate telemetry — it never creates the device identifier; activation
    /// is lazy and performed by <see cref="EnsureActivated"/>. Called once at process startup. See #1701.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Activates telemetry lazily: on first call, creates the persistent <see cref="DeviceId"/> and the per-channel salts
    /// (and sets the default reporting actions) in the global configuration. Subsequent calls only ensure the monthly
    /// rotation/back-fill. This is called by the reporters at the moment they actually report, so that a process which
    /// never reports (e.g. because every context is opted out) never writes anything to the global configuration and
    /// never creates a device identifier. Idempotent and thread-safe.
    /// </summary>
    void EnsureActivated();

    /// <summary>
    /// Gets a value indicating whether telemetry has been activated, i.e. a device identifier has been created (see
    /// <see cref="EnsureActivated"/>). Activation is lazy, so this is <c>false</c> on a machine that has never reported
    /// telemetry (e.g. one that only ever builds opted-out repositories). See #1701.
    /// </summary>
    bool IsActivated { get; }

    /// <summary>
    /// Registers a <paramref name="callback"/> invoked once when telemetry is activated <b>during the current process</b>
    /// (see <see cref="EnsureActivated"/>). Unlike a plain event, this avoids a registration race: if telemetry has
    /// already been activated by the current process when the callback is registered, the callback is invoked
    /// immediately; otherwise it is invoked when activation happens. If telemetry was activated only in a previous
    /// session (the device identifier already exists on disk but no activation happened this run), the callback is not
    /// invoked — the welcome page and the first-run notice were already shown then. Used to defer those until telemetry
    /// is actually collected. See #1701.
    /// </summary>
    void OnActivated( Action callback );

    /// <summary>
    /// Enables (sets every category to <see cref="ReportingAction.Yes"/>) or disables (<see cref="ReportingAction.No"/>)
    /// all telemetry scenarios at once. This backs the global telemetry on/off switch (e.g. the <c>metalama telemetry
    /// enable</c> / <c>disable</c> commands).
    /// </summary>
    void SetStatus( bool enabled );

    /// <summary>
    /// Enables (sets to <see cref="ReportingAction.Yes"/>) or disables (<see cref="ReportingAction.No"/>) a single
    /// telemetry <paramref name="scenario"/> independently of the other scenarios. This backs the per-category
    /// "automatically report all …" checkbox, which enables a category's auto-send without touching usage telemetry. See #1672, #1674.
    /// </summary>
    void SetStatus( TelemetryScenario scenario, bool enabled );

    /// <summary>
    /// Gets the anonymized device identifier used to correlate telemetry reports, or <see cref="System.Guid.Empty"/>
    /// when telemetry has not been activated (see <see cref="IsActivated"/>). It is rotated monthly together with the
    /// salts. Do not hash with it directly — use the per-channel salts (<see cref="MatomoSalt"/>,
    /// <see cref="UsageTrackingSalt"/>, <see cref="ExceptionReportingSalt"/>). See issue #1668.
    /// </summary>
    Guid DeviceId { get; }

    /// <summary>
    /// Gets the effective <see cref="ReportingAction"/> for the given <paramref name="scenario"/>: the configured
    /// per-category action, gated by the process-level checks. It returns <see cref="ReportingAction.No"/> whenever
    /// telemetry is disabled at the process level (see <see cref="IsGloballyEnabled"/>), regardless of the configured
    /// value. This is the universal way to query telemetry enablement and the only one valid for the exception and
    /// performance scenarios, where the action carries the ASK distinction (<see cref="ReportingAction.Default"/> =
    /// capture + ask, <see cref="ReportingAction.Yes"/> = capture + auto-send, <see cref="ReportingAction.No"/> = do not
    /// even capture or ask). See #1674, #1701.
    /// </summary>
    ReportingAction GetEffectiveReportingAction( TelemetryScenario scenario );

    /// <summary>
    /// A convenience over <see cref="GetEffectiveReportingAction"/> for scenarios that have no ASK state — i.e.
    /// <see cref="TelemetryScenario.Usage"/> and <see cref="TelemetryScenario.Rss"/>, where telemetry is simply
    /// collected and sent (opt-out). Equivalent to <c>GetEffectiveReportingAction(scenario) != ReportingAction.No</c>.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <paramref name="scenario"/> can be in an ASK state (<see cref="TelemetryScenario.Exception"/> or
    /// <see cref="TelemetryScenario.Performance"/>), for which a boolean is ambiguous — use
    /// <see cref="GetEffectiveReportingAction"/> instead.
    /// </exception>
    bool IsEnabled( TelemetryScenario scenario );

    /// <summary>
    /// Gets a value indicating whether telemetry is enabled at the process level — i.e. the current application supports
    /// telemetry, the process is not unattended, and the user has not opted out through the environment variable. Unlike
    /// <see cref="IsEnabled"/>, this is independent of the per-category <see cref="ReportingAction"/>. Exception/performance
    /// reports are captured locally (and a toast is shown) when this is <c>true</c> and the category is not
    /// <see cref="ReportingAction.No"/> (<see cref="ReportingAction.Default"/> = capture + ask; <see cref="ReportingAction.Yes"/>
    /// = capture + ask + auto-send; <see cref="ReportingAction.No"/> = not captured); auto-send is additionally gated on
    /// the category being <see cref="ReportingAction.Yes"/>. See #1674, #1701.
    /// </summary>
    bool IsGloballyEnabled { get; }

    /// <summary>
    /// Regenerates the <see cref="DeviceId"/> and the per-channel salts, so that subsequent telemetry can no longer be
    /// correlated with reports sent under the previous identifier. See issue #1668.
    /// </summary>
    void ResetDeviceId();

    /// <summary>
    /// Clears the record of already-reported exception/performance issues, so that an issue which was previously
    /// reported (and therefore deduplicated by <see cref="IExceptionReporter"/>) is captured and surfaced again.
    /// This is primarily a testing aid for exercising the exception-report flow. See #1674.
    /// </summary>
    void ResetReportedIssues();

    /// <summary>
    /// Gets the salt used to hash identifiers sent to the third-party analytics platform (Matomo).
    /// </summary>
    long MatomoSalt { get; }

    /// <summary>
    /// Gets the salt used to hash identifiers sent only to the first-party diagnostic store (bits) by the
    /// usage-tracking channel (the license-audit report), never to the third-party analytics platform (Matomo).
    /// Keying usage-tracking identifiers with this salt instead of <see cref="MatomoSalt"/> makes the Matomo
    /// pseudonym mathematically unjoinable to our usage-tracking data, and keeping it distinct from
    /// <see cref="ExceptionReportingSalt"/> keeps the two first-party channels mutually unjoinable too.
    /// It is rotated together with <see cref="MatomoSalt"/> and <see cref="DeviceId"/>. See issue #1668.
    /// </summary>
    long UsageTrackingSalt { get; }

    /// <summary>
    /// Gets the salt used to hash identifiers sent only to the first-party diagnostic store (bits) by the
    /// exception-reporting channel, never to the third-party analytics platform (Matomo). Keying exception
    /// identifiers with this salt instead of <see cref="MatomoSalt"/> makes the Matomo pseudonym mathematically
    /// unjoinable to our exception data, and keeping it distinct from <see cref="UsageTrackingSalt"/> keeps the
    /// two first-party channels mutually unjoinable too. It is rotated together with <see cref="MatomoSalt"/>
    /// and <see cref="DeviceId"/>. See issue #1668.
    /// </summary>
    long ExceptionReportingSalt { get; }
}

public enum TelemetryScenario
{
    None,
    Usage,
    Exception,
    Performance,
    Rss
}