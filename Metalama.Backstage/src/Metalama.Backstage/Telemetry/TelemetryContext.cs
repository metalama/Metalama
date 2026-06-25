// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using System;
using System.Collections.Immutable;

namespace Metalama.Backstage.Telemetry;

/// <inheritdoc cref="ITelemetryContext"/>
internal sealed class TelemetryContext : ITelemetryContext
{
    private readonly ITelemetryPolicy _policy;
    private readonly IUsageReporter _usageReporter;
    private readonly IExceptionCapturer _exceptionCapturer;
    private readonly LocalExceptionReporter? _localExceptionReporter;

    public TelemetryContext(
        ITelemetryPolicy policy,
        IUsageReporter usageReporter,
        IExceptionCapturer exceptionCapturer,
        LocalExceptionReporter? localExceptionReporter )
    {
        this._policy = policy;
        this._usageReporter = usageReporter;
        this._exceptionCapturer = exceptionCapturer;
        this._localExceptionReporter = localExceptionReporter;
    }

    public ImmutableArray<TelemetryContextWarning> Warnings => this._policy.Warnings;

    public bool IsTelemetryEnabled( TelemetryScenario scenario )

        // The policy is the single authority: it combines the repository opt-out (metalama.json), the process-level gates
        // and the per-category action in telemetry.json. A scenario is active when the policy resolves to anything other
        // than No. See #1701.
        => this._policy.GetReportingAction( scenario ) != ReportingAction.No;

    public IUsageSession StartUsageSession( string kind, string? projectName = null )
        => this.IsTelemetryEnabled( TelemetryScenario.Usage ) ? this._usageReporter.StartSession( kind, projectName ) : NullUsageSession.Instance;

    public void ReportException(
        Exception exception,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
        bool writeLocalReport = true,
        IExceptionAdapter? exceptionAdapter = null )
        => this.ReportException( ExceptionClassifier.Classify( exception ), exceptionReportingKind, writeLocalReport, exceptionAdapter );

    public void ReportException(
        ClassifiedException classifiedException,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
        bool writeLocalReport = true,
        IExceptionAdapter? exceptionAdapter = null )
    {
        var scenario = exceptionReportingKind == ExceptionReportingKind.Exception ? TelemetryScenario.Exception : TelemetryScenario.Performance;

        var reportingAction = this._policy.GetReportingAction( scenario );

        if ( reportingAction != ReportingAction.No )
        {
            // Telemetry is enabled for this scenario: capture the report (the capturer also writes the local crash report
            // when writeLocalReport is true and this is an exception).
            this._exceptionCapturer.Capture( classifiedException, exceptionReportingKind, reportingAction, writeLocalReport, exceptionAdapter );
        }
        else if ( writeLocalReport && exceptionReportingKind == ExceptionReportingKind.Exception )
        {
            // Disabled / context-less / opted-out: never capture or send telemetry, but still write the local crash
            // report for support (it is local diagnostics, not telemetry) — unless the caller already wrote its own
            // (writeLocalReport == false). Only the exception channel produces a local report; performance reports have
            // no local rendering. See #1701.
            this._localExceptionReporter?.ReportException( classifiedException.Exception );
        }
    }
}
