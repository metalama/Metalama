// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Utilities;
using System;

namespace Metalama.Backstage.Telemetry;

internal sealed class TelemetryConfigurationService : ITelemetryConfigurationService
{
    public const string OptOutEnvironmentVariable = "METALAMA_TELEMETRY_OPT_OUT";
    private readonly IConfigurationManager _configurationManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly RandomNumberGenerator _randomNumberGenerator;

    private bool _isUsageTelemetryEnabled;
    private bool _isPerformanceTelemetryEnabled;
    private bool _isExceptionTelemetryEnabled;
    private bool _isGloballyEnabled;
    private bool _initialized;

    // Tests use this constructor to supply a constant Guid.
    internal TelemetryConfigurationService( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._configurationManager.ConfigurationFileChanged += this.OnConfigurationChanged;
        this._logger = this._serviceProvider.GetLoggerFactory().Telemetry();
        this._dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._dateTimeProvider.DateChanged += this.OnDateChanged;
        this._randomNumberGenerator = serviceProvider.GetRequiredBackstageService<RandomNumberGenerator>();
    }

    private void OnDateChanged()
    {
        // Make sure we rotate the DeviceId and Salt consistently every month.
        this.Initialize();
    }

    public long Salt { get; private set; }

    private void OnConfigurationChanged( ConfigurationFile configuration )
    {
        if ( configuration is TelemetryConfiguration telemetryConfiguration )
        {
            this.ReadConfiguration( telemetryConfiguration );
        }
    }

    private bool IsGloballyEnabled()
    {
        // Check if the current application supports telemetry.
        var applicationInfo = this._serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
        var isApplicationTelemetryEnabled = applicationInfo.IsTelemetryEnabled;

        if ( !isApplicationTelemetryEnabled )
        {
            this._logger.Trace?.Log( $"Telemetry is disabled for '{applicationInfo.Name} {applicationInfo.PackageVersion}'." );

            return false;
        }

        // Check if the current process is unattended.
        if ( applicationInfo.IsUnattendedProcess( this._serviceProvider.GetLoggerFactory() ) )
        {
            this._logger.Trace?.Log( $"Telemetry is disabled because the current process is unattended." );

            return false;
        }

        // Check if there is an environment variable opt-out.
        var telemetryOptOutEnvironmentVariableValue = this._serviceProvider.GetRequiredBackstageService<IEnvironmentVariableProvider>()
            .GetEnvironmentVariable( OptOutEnvironmentVariable );

        var isTelemetryOptedOut = !string.IsNullOrEmpty( telemetryOptOutEnvironmentVariableValue );

        if ( bool.TryParse( telemetryOptOutEnvironmentVariableValue, out var parsedBool ) && !parsedBool )
        {
            isTelemetryOptedOut = false;
        }

        if ( int.TryParse( telemetryOptOutEnvironmentVariableValue, out var parsedInt ) && parsedInt == 0 )
        {
            isTelemetryOptedOut = false;
        }

        if ( isTelemetryOptedOut )
        {
            this._logger.Trace?.Log( $"Telemetry is disabled by the opt-out environment variable." );

            return false;
        }

        return true;
    }

    private void ReadConfiguration( TelemetryConfiguration configuration )
    {
        if ( this._isGloballyEnabled )
        {
            this._isExceptionTelemetryEnabled = configuration.ExceptionReportingAction != ReportingAction.No;
            this._isPerformanceTelemetryEnabled = configuration.PerformanceProblemReportingAction != ReportingAction.No;
            this._isUsageTelemetryEnabled = configuration.UsageReportingAction != ReportingAction.No;
        }

        // We should not have null values here because Initialize sets it.
        this.Salt = configuration.Salt ?? 0;
        this.DeviceId = configuration.DeviceId ?? Guid.Empty;
    }

    public void Initialize()
    {
        this._configurationManager.UpdateIf<TelemetryConfiguration>(
            c => c.DeviceId == null,
            c => c with
            {
                DeviceId = this._randomNumberGenerator.NextGuid(),
                UsageReportingAction = ReportingAction.Yes,
                PerformanceProblemReportingAction = ReportingAction.Yes,
                ExceptionReportingAction = ReportingAction.Yes,

                // Make sure we don't upload telemetry data on the first second of use.
                // Since first-time users are likely not to use the software for more than a few minutes, 
                // configure so that we will upload data in 15 minutes.
                LastUploadTime = this._dateTimeProvider.UtcNow.AddDays( -1 ).AddMinutes( 15 ),
                Salt = this._randomNumberGenerator.NextInt64(),
                LastSaltChangeTime = this._dateTimeProvider.UtcNow
            } );

        // We rotate telemetry ids and salt on the first Monday of the month to make sure that
        // weekly aggregates are correct because they are the most important.
        var firstOfMonth = this._dateTimeProvider.UtcNow.Date.GetFirstMondayOfMonth();

        var rotated = this._configurationManager.UpdateIf<TelemetryConfiguration>(
            c => c.Salt == null || c.LastSaltChangeTime == null || (this._dateTimeProvider.UtcNow >= firstOfMonth && c.LastSaltChangeTime.Value < firstOfMonth),
            c => c with
            {
                Salt = this._randomNumberGenerator.NextInt64(),
                DeviceId = this._randomNumberGenerator.NextGuid(),
                LastSaltChangeTime = this._dateTimeProvider.UtcNow
            } );

        if ( rotated )
        {
            this._logger.Trace?.Log( "Telemetry IDs and salt were rotated." );
        }

        this._isGloballyEnabled = this.IsGloballyEnabled();
        this.ReadConfiguration( this._configurationManager.Get<TelemetryConfiguration>() );
        this._initialized = true;
    }

    public void SetStatus( bool enabled )
    {
        var reportAction = enabled switch
        {
            true => ReportingAction.Yes,
            false => ReportingAction.No
        };

        this._configurationManager.Update<TelemetryConfiguration>( c => c with
        {
            UsageReportingAction = reportAction,
            ExceptionReportingAction = reportAction,
            PerformanceProblemReportingAction = reportAction
        } );

        if ( this._isGloballyEnabled )
        {
            this._isExceptionTelemetryEnabled = enabled;
            this._isUsageTelemetryEnabled = enabled;
            this._isPerformanceTelemetryEnabled = enabled;
        }
    }

    public Guid DeviceId { get; private set; }

    public bool IsEnabled( TelemetryScenario scenario )
    {
        if ( !this._initialized )
        {
            throw new InvalidOperationException();
        }

        return scenario switch
        {
            TelemetryScenario.Exception => this._isExceptionTelemetryEnabled,
            TelemetryScenario.Performance => this._isPerformanceTelemetryEnabled,
            TelemetryScenario.Usage => this._isUsageTelemetryEnabled,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void ResetDeviceId()
    {
        this._configurationManager.Update<TelemetryConfiguration>( c => c with
        {
            DeviceId = this._randomNumberGenerator.NextGuid(),
            Salt = this._randomNumberGenerator.NextInt64(),
            LastSaltChangeTime = this._dateTimeProvider.UtcNow
        } );
    }
}