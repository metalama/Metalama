// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.UserInterface.Rss;
using Metalama.Backstage.UserInterface.Toasts;
using Metalama.Backstage.Welcome;
using System;
using System.Collections.Immutable;

namespace Metalama.Backstage.Telemetry;

/// <inheritdoc cref="ITelemetryContext"/>
internal sealed class TelemetryContext : ITelemetryContext
{
    private readonly IUsageSessionFactory _usageSessionFactory;
    private readonly IExceptionCapturer _exceptionCapturer;
    private readonly LocalExceptionReporter? _localExceptionReporter;
    private readonly ILogger _logger;
    private readonly WelcomePageService? _welcomePageService;
    private readonly IToastNotificationService? _toastNotificationService;
    private readonly IRssClient? _rssClient;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;
    private readonly IDateTimeProvider _time;
    private readonly IConfigurationManager _configurationManager;

    public TelemetryContext(
        IServiceProvider serviceProvider,
        ITelemetryPolicy policy )
    {
        this.Policy = policy;
        this._usageSessionFactory = serviceProvider.GetRequiredBackstageService<IUsageSessionFactory>();
        this._exceptionCapturer = serviceProvider.GetRequiredBackstageService<IExceptionCapturer>();
        this._localExceptionReporter = serviceProvider.GetBackstageService<LocalExceptionReporter>();
        this._logger = serviceProvider.GetLoggerFactory().Telemetry();
        this._welcomePageService = serviceProvider.GetBackstageService<WelcomePageService>();
        this._toastNotificationService = serviceProvider.GetBackstageService<IToastNotificationService>();
        this._rssClient = serviceProvider.GetBackstageService<IRssClient>();
        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
        this._time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
    }

    public ITelemetryPolicy Policy { get; }

    public ImmutableArray<TelemetryContextWarning> Warnings => this.Policy.Warnings;

    public IUsageSession StartUsageSession( string kind, string? projectName = null )
    {
        if ( this.Policy.GetConsent( TelemetryScenario.Usage ) != TelemetryConsent.No )
        {
            this.EnableTelemetryIfDefault();

            if ( this._telemetryConfigurationService.GetEffectiveConsent( TelemetryScenario.Usage ) != TelemetryConsent.Yes )
            {
                this._logger.Trace?.Log( $"Telemetry is disabled." );

                return NullUsageSession.Instance;
            }

            // If the project name is not provided, we use the kind as the key
            // to determine if the session should be reported.
            projectName ??= $"<{kind}>";

            return this._usageSessionFactory.CreateSession( kind, this.ShouldCollectMetrics( projectName ) );
        }
        else
        {
            return NullUsageSession.Instance;
        }
    }

    private bool ShouldCollectMetrics( string projectName )
    {
        // Serializes the in-process callers of ShouldCollectMetrics. This service is a per-process singleton, so when a
        // single process compiles many projects concurrently (e.g. the design-time analysis service or an in-process build),
        // all those threads would otherwise race on the shared TelemetryConfiguration timestamp and exhaust the
        // optimistic-concurrency retries in UpdateIf. Serializing them removes this self-contention, which is the dominant
        // source. It is not a full guarantee: occasional retries remain possible from other (infrequent) TelemetryConfiguration
        // writers in the same process, or from other processes (still bounded by the cross-process mutex in ConfigurationManager).
        lock ( this._usageSessionFactory.Sync )
        {
            var now = this._time.UtcNow;

            var configuration = this._configurationManager.Get<TelemetryConfiguration>();

            if ( configuration.Sessions.TryGetValue( projectName, out var lastReported ) && lastReported.AddDays( 1 ) > now )
            {
                this._logger.Trace?.Log( $"Session of project '{projectName}' should not be reported because it has been reported on {lastReported}." );

                return false;
            }

            return this._configurationManager.UpdateIf<TelemetryConfiguration>(
                c =>
                {
                    if ( c.Sessions.TryGetValue( projectName, out var raceReported ) && raceReported.AddDays( 1 ) > now )
                    {
                        this._logger.Trace?.Log(
                            $"Session of project '{projectName}' should not be reported because it is being reported by a concurrent process." );

                        return false;
                    }

                    return true;
                },
                c =>
                {
                    this._logger.Trace?.Log( $"Session of project '{projectName}' should be reported." );

                    c = c.CleanUp( now.AddDays( -1 ) );
                    c = c with { Sessions = c.Sessions.SetItem( projectName, now ) };

                    return c;
                } );
        }
    }

    private void EnableTelemetryIfDefault()
    {
        // If usage reporting is in the default state, we try to enable it. If we are the one who enable it, we display notifications.
        if ( this._telemetryConfigurationService.CompareExchangeConsent( TelemetryScenario.Usage, TelemetryConsent.Yes, TelemetryConsent.Default ) )
        {
            this._logger.Trace?.Log( $"Enabling telemetry now." );
            this._toastNotificationService?.Show( new ToastNotification( ToastNotificationKinds.TelemetryNotice ) );
            this._welcomePageService?.OpenWelcomePageOnce();
            this._rssClient?.TryEnable();
        }
        else
        {
            this._logger.Trace?.Log( $"Telemetry was in status: {this._telemetryConfigurationService.GetEffectiveConsent( TelemetryScenario.Usage )}." );
        }
    }

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