// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Telemetry;
using System;

namespace Metalama.Backstage.Licensing.Audit;

internal class LicenseAuditManager : ILicenseAuditManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationManager _configurationManager;
    private readonly IApplicationInfo _applicationInfo;
    private readonly IDateTimeProvider _time;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly TelemetryReportUploader _telemetryReportUploader;
    private readonly MatomoAuditUploader? _matomoAuditUploader;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;

    public LicenseAuditManager( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._applicationInfo = serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
        this._time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._loggerFactory = serviceProvider.GetLoggerFactory();
        this._logger = this._loggerFactory.Licensing();
        this._telemetryReportUploader = serviceProvider.GetRequiredBackstageService<TelemetryReportUploader>();
        this._matomoAuditUploader = serviceProvider.GetBackstageService<MatomoAuditUploader>();
        this._backgroundTasksService = serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>();
    }

    public void ReportLicense( LicenseConsumptionData license )
    {
        if ( !license.IsAuditable )
        {
            this._logger.Trace?.Log( $"License audit disabled because the license '{license.DisplayName}' is not auditable." );

            return;
        }

        if ( string.IsNullOrEmpty( license.LicenseString ) )
        {
            this._logger.Trace?.Log( $"License audit disabled because the license string is empty." );

            return;
        }

        if ( this._applicationInfo.IsUnattendedProcess( this._loggerFactory ) )
        {
            this._logger.Trace?.Log( "License audit disabled because the current process is unattended." );

            return;
        }

        if ( !this._applicationInfo.IsTelemetryEnabled )
        {
            this._logger.Trace?.Log( $"License audit disabled because telemetry is disabled for the current build." );

            return;
        }

        var report = new LicenseAuditTelemetryReport( this._serviceProvider, license );

        if ( report.ReportedComponent.PackageVersion == null )
        {
            throw new InvalidOperationException( $"Version of '{report.ReportedComponent.Name}' application is unknown." );
        }

        // Perform detailed audit.
        var mustPerformAudit = this._configurationManager.UpdateIf<LicenseAuditConfiguration>(
            c => !c.LastAuditTimes.TryGetValue( report.AuditHashCode, out var lastReportTime )
                 || lastReportTime <= this._time.UtcNow.AddDays( -1 ),
            c => c with { LastAuditTimes = c.LastAuditTimes.SetItem( report.AuditHashCode, this._time.UtcNow ) } );

        if ( !mustPerformAudit )
        {
            this._logger.Trace?.Log( $"License audit disabled because the license '{license.DisplayName}' has been recently audited." );
        }
        else
        {
            this._logger.Trace?.Log( $"Uploading license audit report." );
            this._backgroundTasksService.Enqueue( () => this._telemetryReportUploader.Upload( report ) );
        }

        // Perform aggregate audit to Matomo. We intentionally upload one report per day irrespective of the version used 
        // (which means that the version number being reported may be random if the user uses several version) because
        // we are more interested in having correct aggregates on Matomo than correct version usage statistics.
        if ( this._matomoAuditUploader != null )
        {
            var mustPerformAggregateAudit = this._configurationManager.UpdateIf<LicenseAuditConfiguration>(
                c => c.LastMatomoAuditTime == null || c.LastMatomoAuditTime <= this._time.UtcNow.AddDays( -1 ),
                c => c with { LastMatomoAuditTime = this._time.UtcNow } );
            
            if ( mustPerformAggregateAudit )
            {
                this._backgroundTasksService.Enqueue( () => this._matomoAuditUploader.UploadAsync( report ) );
            }
        }
    }
}