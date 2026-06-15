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
    void Initialize();

    void SetStatus( bool enabled );

    /// <summary>
    /// Sets the <see cref="ReportingAction"/> of a single telemetry <paramref name="scenario"/> (e.g. exceptions or
    /// performance problems) independently of the other scenarios. This backs the per-category "automatically report
    /// all …" checkbox, which enables a category's auto-send without touching usage telemetry. See #1674.
    /// </summary>
    void SetReportingAction( TelemetryScenario scenario, ReportingAction action );

    Guid DeviceId { get; }

    bool IsEnabled( TelemetryScenario scenario );

    void ResetDeviceId();

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