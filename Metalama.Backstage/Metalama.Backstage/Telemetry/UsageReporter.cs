// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using System;

namespace Metalama.Backstage.Telemetry;

internal class UsageReporter : IUsageReporter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationManager _configurationManager;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;
    private readonly IDateTimeProvider _time;
    private readonly ILogger _logger;

    public bool IsUsageReportingEnabled
        => this._telemetryConfigurationService.IsEnabled
           && this._configurationManager.Get<TelemetryConfiguration>().UsageReportingAction == ReportingAction.Yes;

    public UsageReporter( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
        this._time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._logger = serviceProvider.GetLoggerFactory().Telemetry();
    }

    public bool ShouldReportSession( string projectName )
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

    public IUsageSession? StartSession( string kind )
    {
        if ( !this.IsUsageReportingEnabled )
        {
            return null;
        }

        return new UsageSession( this._serviceProvider, kind );
    }
}