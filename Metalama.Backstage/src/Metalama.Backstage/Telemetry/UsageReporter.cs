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
        var now = this._time.UtcNow;

        if ( !this.IsUsageReportingEnabled )
        {
            return false;
        }

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