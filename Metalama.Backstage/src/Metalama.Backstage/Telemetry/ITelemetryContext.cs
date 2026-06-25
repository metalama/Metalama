// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using System;
using System.Collections.Immutable;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// A telemetry context, obtained from <see cref="ITelemetryService.OpenContext"/> for a directory (a project, solution
/// or repository directory). Reporting telemetry always goes through a context, so that the repository-scoped opt-out
/// (<c>metalama.json</c>) and the process-level gates are enforced by construction: a context whose
/// <see cref="IsTelemetryEnabled"/> is <c>false</c> never collects or sends anything. Local crash reports are written
/// regardless (they are local support data, not telemetry). See #1701.
/// </summary>
[PublicAPI]
public interface ITelemetryContext
{
    /// <summary>
    /// Determines whether telemetry may be collected and sent for the given <paramref name="scenario"/> in this context.
    /// This is <c>true</c> only when the repository has not opted out through <c>metalama.json</c> and the scenario is
    /// enabled at the process and per-category level (resolved through
    /// <see cref="ITelemetryConfigurationService.GetEffectiveReportingAction"/>, which — unlike
    /// <see cref="ITelemetryConfigurationService.IsEnabled"/> — also covers the ASK-capable Exception/Performance
    /// scenarios) — i.e. telemetry is actually activated for that specific scenario.
    /// </summary>
    bool IsTelemetryEnabled( TelemetryScenario scenario );

    /// <summary>
    /// Gets the warnings produced while resolving the repository configuration (for example, a misplaced or malformed
    /// <c>metalama.json</c>). The caller should surface these as compiler warnings or IDE notifications, pointing the
    /// location at <see cref="TelemetryContextWarning.FilePath"/>.
    /// </summary>
    ImmutableArray<TelemetryContextWarning> Warnings { get; }

    /// <summary>
    /// Starts a usage-telemetry session. Returns a no-op session when <see cref="IsTelemetryEnabled"/> is <c>false</c>.
    /// </summary>
    IUsageSession StartUsageSession( string kind, string? projectName = null );

    /// <summary>
    /// Reports an exception. The local crash report is written (local support data) unless <paramref name="writeLocalReport"/>
    /// is <c>false</c> — which a caller that has already written its own report passes to avoid a duplicate. The telemetry
    /// capture, review-first toast and upload happen only when <see cref="IsTelemetryEnabled"/> is <c>true</c> for the
    /// corresponding scenario.
    /// </summary>
    void ReportException(
        Exception exception,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
        bool writeLocalReport = true,
        IExceptionAdapter? exceptionAdapter = null );

    /// <inheritdoc cref="ReportException(System.Exception,Metalama.Backstage.Telemetry.ExceptionReportingKind,bool,Metalama.Backstage.Telemetry.IExceptionAdapter?)"/>
    void ReportException(
        ClassifiedException classifiedException,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
        bool writeLocalReport = true,
        IExceptionAdapter? exceptionAdapter = null );
}
