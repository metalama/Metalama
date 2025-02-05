// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using System;

namespace Metalama.Backstage.Telemetry;

internal class TelemetryConfigurationService : ITelemetryConfigurationService
{
    private readonly IConfigurationManager _configurationManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly Guid _newDeviceId;
    private bool? _isEnabled;

    internal TelemetryConfigurationService( IServiceProvider serviceProvider, Guid? newDeviceId = null )
        : this( serviceProvider, newDeviceId ?? Guid.NewGuid() ) { }

    // Tests use this constructor to supply a constant Guid.
    internal TelemetryConfigurationService( IServiceProvider serviceProvider, Guid newDeviceId )
    {
        this._serviceProvider = serviceProvider;
        this._newDeviceId = newDeviceId;
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
    }

    private bool IsEnabledCore()
    {
        var loggerFactory = this._serviceProvider.GetLoggerFactory();
        var logger = loggerFactory.Telemetry();

        var applicationInfo = this._serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
        var isApplicationTelemetryEnabled = applicationInfo.IsTelemetryEnabled;

        if ( !isApplicationTelemetryEnabled )
        {
            logger.Trace?.Log( $"Telemetry is disabled for '{applicationInfo.Name} {applicationInfo.PackageVersion}'." );

            return false;
        }

        var telemetryOptOutEnvironmentVariableValue = this._serviceProvider.GetRequiredBackstageService<IEnvironmentVariableProvider>()
            .GetEnvironmentVariable( "METALAMA_TELEMETRY_OPT_OUT" );

        var isTelemetryOptedOut = !string.IsNullOrEmpty( telemetryOptOutEnvironmentVariableValue );

        if ( isTelemetryOptedOut )
        {
            logger.Trace?.Log( $"Telemetry is disabled by the opt-out environment variable." );

            return false;
        }

        if ( applicationInfo.IsUnattendedProcess( loggerFactory ) )
        {
            logger.Trace?.Log( $"Telemetry is disabled because the current process is unattended." );

            return false;
        }

        return true;
    }

    public void SetStatus( bool? enabled )
    {
        var reportAction = enabled switch
        {
            null => ReportingAction.Ask,
            true => ReportingAction.Yes,
            false => ReportingAction.No
        };

        this._configurationManager.Update<TelemetryConfiguration>(
            c => c with { UsageReportingAction = reportAction, ExceptionReportingAction = reportAction, PerformanceProblemReportingAction = reportAction } );
    }

    public Guid DeviceId
    {
        get
        {
            var configuration = this._configurationManager.Get<TelemetryConfiguration>();

            if ( configuration.DeviceId == null )
            {
                this._configurationManager.UpdateIf<TelemetryConfiguration>( c => c.DeviceId == null, c => c with { DeviceId = this._newDeviceId } );
                configuration = this._configurationManager.Get<TelemetryConfiguration>();
            }

            return configuration.DeviceId!.Value;
        }
    }

    public bool IsEnabled => this._isEnabled ??= this.IsEnabledCore();

    public void ResetDeviceId()
    {
        this._configurationManager.Update<TelemetryConfiguration>( c => c with { DeviceId = Guid.NewGuid() } );
    }
}