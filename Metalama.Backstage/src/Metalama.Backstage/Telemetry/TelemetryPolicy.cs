// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Collections.Immutable;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// The default <see cref="ITelemetryPolicy"/>, built by <see cref="ITelemetryService.GetPolicy"/> for a directory. It
/// combines the repository-scoped <c>metalama.json</c> opt-out with the process-level and per-category gates resolved by
/// <see cref="ITelemetryConfigurationService.GetEffectiveReportingAction"/>. It is public so that a host-supplied policy
/// (which, under pure replacement, would otherwise lose the <c>metalama.json</c> opt-out) can delegate to it. See #1701.
/// </summary>
[PublicAPI]
public sealed class TelemetryPolicy : ITelemetryPolicy
{
    private readonly bool _isRepositoryTelemetryDisabled;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;

    public TelemetryPolicy(
        bool isRepositoryTelemetryDisabled,
        ITelemetryConfigurationService telemetryConfigurationService,
        ImmutableArray<TelemetryContextWarning> warnings )
    {
        this._isRepositoryTelemetryDisabled = isRepositoryTelemetryDisabled;
        this._telemetryConfigurationService = telemetryConfigurationService;
        this.Warnings = warnings.IsDefault ? ImmutableArray<TelemetryContextWarning>.Empty : warnings;
    }

    public ImmutableArray<TelemetryContextWarning> Warnings { get; }

    public ReportingAction GetReportingAction( TelemetryScenario scenario )

        // The repository opt-out (metalama.json) vetoes every scenario. Otherwise we return the effective reporting
        // action, which combines the process-level gates with the per-category action in telemetry.json and preserves
        // the ASK distinction for the exception/performance scenarios. See #1701.
        => this._isRepositoryTelemetryDisabled
            ? ReportingAction.No
            : this._telemetryConfigurationService.GetEffectiveReportingAction( scenario );
}
