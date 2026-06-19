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
    private readonly bool _isRepositoryTelemetryDisabled;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;
    private readonly IUsageReporter _usageReporter;
    private readonly IExceptionReporter _exceptionReporter;
    private readonly LocalExceptionReporter? _localExceptionReporter;

    public ImmutableArray<TelemetryContextWarning> Warnings { get; }

    public TelemetryContext(
        bool isRepositoryTelemetryDisabled,
        ImmutableArray<TelemetryContextWarning> warnings,
        ITelemetryConfigurationService telemetryConfigurationService,
        IUsageReporter usageReporter,
        IExceptionReporter exceptionReporter,
        LocalExceptionReporter? localExceptionReporter )
    {
        this._isRepositoryTelemetryDisabled = isRepositoryTelemetryDisabled;
        this.Warnings = warnings.IsDefault ? ImmutableArray<TelemetryContextWarning>.Empty : warnings;
        this._telemetryConfigurationService = telemetryConfigurationService;
        this._usageReporter = usageReporter;
        this._exceptionReporter = exceptionReporter;
        this._localExceptionReporter = localExceptionReporter;
    }

    public bool IsTelemetryEnabled( TelemetryScenario scenario )

        // The repository opt-out (metalama.json) vetoes every scenario. Otherwise the channel is active when the
        // effective reporting action is not No (which combines the process-level gates with the per-category action in
        // telemetry.json). We use GetEffectiveReportingAction rather than IsEnabled because it is valid for every
        // scenario, including the ASK-capable exception/performance ones. The short-circuit also means a disabled /
        // context-less context never touches the configuration service. See #1701.
        => !this._isRepositoryTelemetryDisabled && this._telemetryConfigurationService.GetEffectiveReportingAction( scenario ) != ReportingAction.No;

    public IUsageSession StartUsageSession( string kind, string? projectName = null )
        => this.IsTelemetryEnabled( TelemetryScenario.Usage ) ? this._usageReporter.StartSession( kind, projectName ) : NullUsageSession.Instance;

    public void ReportException(
        Exception exception,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
        string? localReportPath = null,
        IExceptionAdapter? exceptionAdapter = null )
        => this.ReportException( ExceptionClassifier.Classify( exception ), exceptionReportingKind, localReportPath, exceptionAdapter );

    public void ReportException(
        ClassifiedException classifiedException,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
        string? localReportPath = null,
        IExceptionAdapter? exceptionAdapter = null )
    {
        var scenario = exceptionReportingKind == ExceptionReportingKind.Exception ? TelemetryScenario.Exception : TelemetryScenario.Performance;

        if ( this.IsTelemetryEnabled( scenario ) )
        {
            // Telemetry is enabled for this scenario: capture the report for telemetry (this path also writes the local
            // crash report).
            this._exceptionReporter.ReportException( classifiedException, exceptionReportingKind, localReportPath, exceptionAdapter );
        }
        else
        {
            // Disabled / context-less / opted-out: never capture or send telemetry, but still write the local crash
            // report for support (it is local diagnostics, not telemetry). Only the exception channel produces a local
            // report; performance reports have no local rendering. See #1701.
            if ( exceptionReportingKind == ExceptionReportingKind.Exception )
            {
                this._localExceptionReporter?.ReportException( classifiedException.Exception, localReportPath );
            }
        }
    }
}
