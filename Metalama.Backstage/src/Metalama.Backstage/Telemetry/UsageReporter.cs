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

namespace Metalama.Backstage.Telemetry;

internal sealed class UsageReporter : IUsageReporter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationManager _configurationManager;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;
    private readonly IDateTimeProvider _time;
    private readonly ILogger _logger;
    private readonly WelcomePageService? _welcomePageService;
    private readonly IToastNotificationService? _toastNotificationService;
    private readonly IRssClient? _rssClient;

    // Serializes the in-process callers of ShouldCollectMetrics. This service is a per-process singleton, so when a
    // single process compiles many projects concurrently (e.g. the design-time analysis service or an in-process build),
    // all those threads would otherwise race on the shared TelemetryConfiguration timestamp and exhaust the
    // optimistic-concurrency retries in UpdateIf. Serializing them removes this self-contention, which is the dominant
    // source. It is not a full guarantee: occasional retries remain possible from other (infrequent) TelemetryConfiguration
    // writers in the same process, or from other processes (still bounded by the cross-process mutex in ConfigurationManager).
    private readonly object _sync = new();

    public bool IsUsageReportingEnabled => this._telemetryConfigurationService.GetEffectiveConsent( TelemetryScenario.Usage ) == TelemetryConsent.Yes;

    public UsageReporter( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
        this._time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._logger = serviceProvider.GetLoggerFactory().Telemetry();
        this._welcomePageService = serviceProvider.GetBackstageService<WelcomePageService>();
        this._toastNotificationService = serviceProvider.GetBackstageService<IToastNotificationService>();
        this._rssClient = serviceProvider.GetBackstageService<IRssClient>();
    }

    private bool ShouldCollectMetrics( string projectName )
    {
        if ( !this.IsUsageReportingEnabled )
        {
            return false;
        }

        // Serialize the read-modify-write so concurrent in-process compilations do not race on the configuration timestamp.
        lock ( this._sync )
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

    public IUsageSession StartSession( string kind, string? projectName = null )
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

        if ( !this.IsUsageReportingEnabled )
        {
            this._logger.Trace?.Log( $"Telemetry is disabled." );

            return NullUsageSession.Instance;
        }

        // We are about to report usage, so make sure telemetry is activated (the DeviceId and salts exist). Activation
        // is lazy so that a process which never reports never creates a device identifier. See #1701.
        this._telemetryConfigurationService.EnsureActivated();

        // If the project name is not provided, we use the kind as the key
        // to determine if the session should be reported.
        projectName ??= $"<{kind}>";

        return new UsageSession( this._serviceProvider, kind, this.ShouldCollectMetrics( projectName ) );
    }
}