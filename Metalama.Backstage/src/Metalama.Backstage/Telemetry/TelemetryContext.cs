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
    private readonly IUsageReporter _usageReporter;
    private readonly IExceptionCapturer _exceptionCapturer;
    private readonly LocalExceptionReporter? _localExceptionReporter;

    public TelemetryContext(
        ITelemetryPolicy policy,
        IUsageReporter usageReporter,
        IExceptionCapturer exceptionCapturer,
        LocalExceptionReporter? localExceptionReporter )
    {
        this.Policy = policy;
        this._usageReporter = usageReporter;
        this._exceptionCapturer = exceptionCapturer;
        this._localExceptionReporter = localExceptionReporter;
    }

    public ITelemetryPolicy Policy { get; }

    public ImmutableArray<TelemetryContextWarning> Warnings => this.Policy.Warnings;

    public IUsageSession StartUsageSession( string kind, string? projectName = null )
        => this.Policy.GetConsent( TelemetryScenario.Usage ) != TelemetryConsent.No
            ? this._usageReporter.StartSession( kind, projectName )
            : NullUsageSession.Instance;

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

        var consent = this.Policy.GetConsent( scenario );

        if ( consent != TelemetryConsent.No )
        {
            // Telemetry is enabled for this scenario: capture the report (the capturer also writes the local crash report
            // when writeLocalReport is true and this is an exception).
            this._exceptionCapturer.Capture( classifiedException, exceptionReportingKind, consent, writeLocalReport, exceptionAdapter );
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