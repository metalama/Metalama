// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using System;

namespace Metalama.Backstage.Telemetry;

internal sealed class UsageReporter : IUsageReporter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationManager _configurationManager;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;
    private readonly IDateTimeProvider _time;
    private readonly ILogger _logger;

    // Serializes the in-process callers of ShouldCollectMetrics. This service is a per-process singleton, so when a
    // single process compiles many projects concurrently (e.g. the design-time analysis service or an in-process build),
    // all those threads would otherwise race on the shared TelemetryConfiguration timestamp and exhaust the
    // optimistic-concurrency retries in UpdateIf. Serializing them removes this self-contention, which is the dominant
    // source. It is not a full guarantee: occasional retries remain possible from other (infrequent) TelemetryConfiguration
    // writers in the same process, or from other processes (still bounded by the cross-process mutex in ConfigurationManager).
    private readonly object _sync = new();

    public bool IsUsageReportingEnabled => this._telemetryConfigurationService.IsEnabled( TelemetryScenario.Usage );

    public UsageReporter( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
        this._time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._logger = serviceProvider.GetLoggerFactory().Telemetry();
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
        if ( !this.IsUsageReportingEnabled )
        {
            return NullUsageSession.Instance;
        }

        // If the project name is not provided, we use the kind as the key
        // to determine if the session should be reported.
        projectName ??= $"<{kind}>";

        return new UsageSession( this._serviceProvider, kind, this.ShouldCollectMetrics( projectName ) );
    }
}