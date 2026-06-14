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
using System.Collections.Generic;

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
        // Make sure we rotate the DeviceId and the salts (MatomoSalt, UsageTrackingSalt, ExceptionReportingSalt) consistently every month.
        this.Initialize();
    }

    public long MatomoSalt { get; private set; }

    public long UsageTrackingSalt { get; private set; }

    public long ExceptionReportingSalt { get; private set; }

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
        this.MatomoSalt = configuration.MatomoSalt ?? 0;
        this.UsageTrackingSalt = configuration.UsageTrackingSalt ?? 0;
        this.ExceptionReportingSalt = configuration.ExceptionReportingSalt ?? 0;
        this.DeviceId = configuration.DeviceId ?? Guid.Empty;
    }

    public void Initialize()
    {
        this._configurationManager.UpdateIf<TelemetryConfiguration>(
            c => c.DeviceId == null,
            c =>
            {
                var salts = this.GenerateDistinctSalts();

                return c with
                {
                    DeviceId = this._randomNumberGenerator.NextGuid(),
                    UsageReportingAction = ReportingAction.Yes,
                    PerformanceProblemReportingAction = ReportingAction.Yes,
                    ExceptionReportingAction = ReportingAction.Yes,

                    // Make sure we don't upload telemetry data on the first second of use.
                    // Since first-time users are likely not to use the software for more than a few minutes,
                    // configure so that we will upload data in 15 minutes.
                    LastUploadTime = this._dateTimeProvider.UtcNow.AddDays( -1 ).AddMinutes( 15 ),
                    MatomoSalt = salts.Matomo,
                    UsageTrackingSalt = salts.UsageTracking,
                    ExceptionReportingSalt = salts.ExceptionReporting,
                    LastSaltChangeTime = this._dateTimeProvider.UtcNow
                };
            } );

        // We rotate telemetry ids and salts on the first Monday of the month to make sure that
        // weekly aggregates are correct because they are the most important.
        var firstOfMonth = this._dateTimeProvider.UtcNow.Date.GetFirstMondayOfMonth();

        var rotated = this._configurationManager.UpdateIf<TelemetryConfiguration>(
            c => c.MatomoSalt == null || c.LastSaltChangeTime == null || (this._dateTimeProvider.UtcNow >= firstOfMonth && c.LastSaltChangeTime.Value < firstOfMonth),
            c =>
            {
                var salts = this.GenerateDistinctSalts();

                return c with
                {
                    MatomoSalt = salts.Matomo,
                    UsageTrackingSalt = salts.UsageTracking,
                    ExceptionReportingSalt = salts.ExceptionReporting,
                    DeviceId = this._randomNumberGenerator.NextGuid(),
                    LastSaltChangeTime = this._dateTimeProvider.UtcNow
                };
            } );

        if ( rotated )
        {
            this._logger.Trace?.Log( "Telemetry IDs and salts were rotated." );
        }

        // Back-fill the first-party diagnostic salts for configurations created before they were introduced,
        // without rotating the Matomo Salt or the DeviceId (which would reset Matomo visitor continuity). The
        // back-filled salts are distinct from each other and from any salt already present. See #1668.
        this._configurationManager.UpdateIf<TelemetryConfiguration>(
            c => c.DeviceId != null && (c.UsageTrackingSalt == null || c.ExceptionReportingSalt == null),
            c =>
            {
                var existingSalts = new HashSet<long>();

                if ( c.MatomoSalt != null )
                {
                    existingSalts.Add( c.MatomoSalt.Value );
                }

                if ( c.UsageTrackingSalt != null )
                {
                    existingSalts.Add( c.UsageTrackingSalt.Value );
                }

                if ( c.ExceptionReportingSalt != null )
                {
                    existingSalts.Add( c.ExceptionReportingSalt.Value );
                }

                return c with
                {
                    UsageTrackingSalt = c.UsageTrackingSalt ?? this.NextDistinctSalt( existingSalts ),
                    ExceptionReportingSalt = c.ExceptionReportingSalt ?? this.NextDistinctSalt( existingSalts )
                };
            } );

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

        this._configurationManager.Update<TelemetryConfiguration>(
            c => c with { UsageReportingAction = reportAction, ExceptionReportingAction = reportAction, PerformanceProblemReportingAction = reportAction } );

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
            throw new InvalidOperationException( "The service has not been initialized." );
        }

        return scenario switch
        {
            TelemetryScenario.Exception => this._isExceptionTelemetryEnabled,
            TelemetryScenario.Performance => this._isPerformanceTelemetryEnabled,
            TelemetryScenario.Usage => this._isUsageTelemetryEnabled,
            TelemetryScenario.Rss => this._isGloballyEnabled,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void ResetDeviceId()
    {
        this._configurationManager.Update<TelemetryConfiguration>(
            c =>
            {
                var salts = this.GenerateDistinctSalts();

                return c with
                {
                    DeviceId = this._randomNumberGenerator.NextGuid(),
                    MatomoSalt = salts.Matomo,
                    UsageTrackingSalt = salts.UsageTracking,
                    ExceptionReportingSalt = salts.ExceptionReporting,
                    LastSaltChangeTime = this._dateTimeProvider.UtcNow
                };
            } );
    }

    // Generates the three per-channel salts so that they are all non-zero and mutually distinct. A 64-bit CSPRNG
    // makes zero or a collision astronomically unlikely, but we guard against them anyway because the whole point of
    // the per-channel salts is that the resulting pseudonyms are mutually uncorrelatable. See #1668.
    private (long Matomo, long UsageTracking, long ExceptionReporting) GenerateDistinctSalts()
    {
        var salts = new HashSet<long>();

        return (this.NextDistinctSalt( salts ), this.NextDistinctSalt( salts ), this.NextDistinctSalt( salts ));
    }

    // Returns a cryptographically-secure salt that is non-zero and not already present in <paramref name="existingSalts"/>,
    // to which the returned value is added.
    private long NextDistinctSalt( HashSet<long> existingSalts )
    {
        long salt;

        do
        {
            salt = this._randomNumberGenerator.NextCryptographicInt64();
        }
        while ( salt == 0 || !existingSalts.Add( salt ) );

        return salt;
    }
}