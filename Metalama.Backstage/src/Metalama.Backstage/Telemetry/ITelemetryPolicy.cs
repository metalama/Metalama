// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Collections.Immutable;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// Answers the telemetry enablement question for an <see cref="ITelemetryContext"/>. A context obtained from
/// <see cref="ITelemetryService.OpenContext"/> reads enablement exclusively through its policy, so the policy is the
/// single authority that combines the process-level gates, the per-category <c>telemetry.json</c> action and the
/// repository-scoped <c>metalama.json</c> opt-out. The default policy (<see cref="ITelemetryService.GetPolicy"/>)
/// reproduces that combination; a caller may supply its own policy to <see cref="ITelemetryService.OpenContext"/>, which
/// fully replaces the default for that context — the seam through which a host (for example Visual Studio Tools for
/// Metalama) injects its own consent. See #1701.
/// </summary>
[PublicAPI]
public interface ITelemetryPolicy
{
    bool HasRepositoryContext { get; }

    /// <summary>
    /// Gets the effective <see cref="TelemetryConsent"/> for the given <paramref name="scenario"/>. <see cref="TelemetryConsent.No"/>
    /// means the scenario is disabled (not even captured). For the ASK-capable Exception/Performance scenarios,
    /// <see cref="TelemetryConsent.Default"/> means capture + ask and <see cref="TelemetryConsent.Yes"/> means capture +
    /// auto-send; for opt-out scenarios (Usage/Rss) both <see cref="TelemetryConsent.Default"/> and
    /// <see cref="TelemetryConsent.Yes"/> mean enabled.
    /// </summary>
    TelemetryConsent GetConsent( TelemetryScenario scenario );

    ( TelemetryConsent Consent, TelemetryDisabledReason Reason ) GetConsentAndReason( TelemetryScenario scenario );

    /// <summary>
    /// Gets the warnings produced while resolving the configuration backing this policy (for example, a misplaced or
    /// malformed <c>metalama.json</c>). They are surfaced on <see cref="ITelemetryContext.Warnings"/> for the caller to
    /// report as diagnostics.
    /// </summary>
    ImmutableArray<TelemetryContextWarning> Warnings { get; }
}