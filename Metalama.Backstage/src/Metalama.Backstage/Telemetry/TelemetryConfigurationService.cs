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

    private bool ComputeIsGloballyEnabled()
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

    // Caches the salts and device identifier read from the configuration. The per-scenario enablement is NOT cached: it
    // is computed on demand by GetEffectiveReportingAction, which reads the (cached) configuration and applies the
    // process-level gate. See #1674, #1701.
    private void ReadConfiguration( TelemetryConfiguration configuration )
    {
        // We should not have null values here because EnsureActivated sets it.
        this.MatomoSalt = configuration.MatomoSalt ?? 0;
        this.UsageTrackingSalt = configuration.UsageTrackingSalt ?? 0;
        this.ExceptionReportingSalt = configuration.ExceptionReportingSalt ?? 0;
        this.DeviceId = configuration.DeviceId ?? Guid.Empty;
    }

    // Serializes EnsureActivated against itself and against Initialize so the lazy first-run write happens at most once.
    private readonly object _activationSync = new();

    public void Initialize()
    {
        lock ( this._activationSync )
        {
            // Activation is lazy: we do NOT create the DeviceId / salts here. A process that never reports any telemetry
            // (e.g. because every telemetry context is opted out, or it is a context-less process) must never write
            // anything to the global configuration and never create a device identifier. The DeviceId and salts are
            // created on demand by EnsureActivated, which the reporters call when they actually report. See #1701.
            this._isGloballyEnabled = this.ComputeIsGloballyEnabled();

            // If telemetry was already activated in a previous session, keep the monthly rotation / salt back-fill up to
            // date. This never creates a DeviceId for a not-yet-activated configuration (it is a no-op in that case).
            this.RotateDeviceIdAndSaltIfActivated();

            this.ReadConfiguration( this._configurationManager.Get<TelemetryConfiguration>() );
            this._initialized = true;
        }
    }

    public void EnsureActivated()
    {
        bool created;

        lock ( this._activationSync )
        {
            created = this._configurationManager.UpdateIf<TelemetryConfiguration>(
                c => c.DeviceId == null,
                c =>
                {
                    var salts = this.GenerateDistinctSalts();

                    // We only write the activation artifacts here (the device identifier, the salts and the upload
                    // timing). We deliberately do NOT set the per-channel reporting actions: their record defaults
                    // already express the intended policy — usage is opt-out (ReportingAction.Default is treated as
                    // enabled, see ReadConfiguration) and the exception/performance channels are review-first
                    // (ReportingAction.Default, see #1674). Setting them here would clobber any choice the user made
                    // through SetStatus before telemetry was activated (activation is now lazy, so it can run after
                    // an in-product opt-in/opt-out). See #1701, #1674.
                    return c with
                    {
                        DeviceId = this._randomNumberGenerator.NextGuid(),

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

            if ( created )
            {
                this._logger.Trace?.Log( "Telemetry was activated: a device identifier and salts were created." );
            }

            this.RotateDeviceIdAndSaltIfActivated();
            this.ReadConfiguration( this._configurationManager.Get<TelemetryConfiguration>() );
        }

        // Invoke the registered OnActivated callbacks outside the lock so handlers (the welcome page and the first-run
        // telemetry notice) cannot deadlock or re-enter. This runs exactly once: only the thread that created the device
        // identifier sees created == true. Callbacks registered after this point are invoked immediately by OnActivated.
        // See #1701.
        if ( created )
        {
            Action[] callbacks;

            lock ( this._activationCallbackSync )
            {
                this._activatedThisSession = true;
                callbacks = this._onActivatedCallbacks.ToArray();
                this._onActivatedCallbacks.Clear();
            }

            foreach ( var callback in callbacks )
            {
                callback();
            }
        }
    }

    // Performs the monthly rotation of the salt and device ids, but only for an already-activated configuration. It never
    // creates a DeviceId, so calling it on a not-yet-activated configuration is a no-op (telemetry stays dormant).
    private void RotateDeviceIdAndSaltIfActivated()
    {
        if ( this._configurationManager.Get<TelemetryConfiguration>().DeviceId == null )
        {
            return;
        }

        // We rotate telemetry ids and salts on the first Monday of the month to make sure that
        // weekly aggregates are correct because they are the most important.
        var firstOfMonth = this._dateTimeProvider.UtcNow.Date.GetFirstMondayOfMonth();

        var rotated = this._configurationManager.UpdateIf<TelemetryConfiguration>(
            c => c.DeviceId != null
                 && (c.MatomoSalt == null || c.LastSaltChangeTime == null
                                          || (this._dateTimeProvider.UtcNow >= firstOfMonth && c.LastSaltChangeTime.Value < firstOfMonth)),
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
    }

    public void SetStatus( TelemetryScenario scenario, bool enabled )
    {
        var reportAction = enabled ? ReportingAction.Yes : ReportingAction.No;

        this._configurationManager.Update<TelemetryConfiguration>(
            c => scenario switch
            {
                TelemetryScenario.Usage => c with { UsageReportingAction = reportAction },
                TelemetryScenario.Exception => c with { ExceptionReportingAction = reportAction },
                TelemetryScenario.Performance => c with { PerformanceProblemReportingAction = reportAction },
                _ => throw new ArgumentOutOfRangeException( nameof(scenario), scenario, "This telemetry scenario cannot be configured." )
            } );
    }

    public Guid DeviceId { get; private set; }

    public bool IsActivated => this.DeviceId != Guid.Empty;

    // Coordinates OnActivated callbacks with EnsureActivated. _activatedThisSession is set when THIS process performs the
    // activation (the device-id creation): callbacks registered before that are invoked then; callbacks registered after
    // it are invoked immediately. A device id that merely exists from a previous session does not set this flag. See #1701.
    private readonly object _activationCallbackSync = new();
    private readonly List<Action> _onActivatedCallbacks = new();
    private bool _activatedThisSession;

    public void OnActivated( Action callback )
    {
        bool invokeNow;

        lock ( this._activationCallbackSync )
        {
            if ( this._activatedThisSession )
            {
                invokeNow = true;
            }
            else
            {
                this._onActivatedCallbacks.Add( callback );
                invokeNow = false;
            }
        }

        if ( invokeNow )
        {
            callback();
        }
    }

    public bool IsGloballyEnabled
    {
        get
        {
            if ( !this._initialized )
            {
                throw new InvalidOperationException( "The service has not been initialized." );
            }

            return this._isGloballyEnabled;
        }
    }

    public ReportingAction GetEffectiveReportingAction( TelemetryScenario scenario )
    {
        if ( !this._initialized )
        {
            throw new InvalidOperationException( "The service has not been initialized." );
        }

        // The process-level gate overrides the configured per-category action: when telemetry is disabled for the
        // process (unattended, opt-out environment variable, or unsupported application), every scenario is effectively
        // No. See #1701.
        if ( !this._isGloballyEnabled )
        {
            return ReportingAction.No;
        }

        var configuration = this._configurationManager.Get<TelemetryConfiguration>();

        return scenario switch
        {
            // The news feed counts as usage telemetry for opt-out purposes: an in-product opt-out (SetStatus(false))
            // must stop the RSS fetch, just like the opt-out environment variable. See #1670.
            TelemetryScenario.Usage or TelemetryScenario.Rss => configuration.UsageReportingAction,
            TelemetryScenario.Exception => configuration.ExceptionReportingAction,
            TelemetryScenario.Performance => configuration.PerformanceProblemReportingAction,
            _ => throw new ArgumentOutOfRangeException( nameof(scenario), scenario, null )
        };
    }

    public bool IsEnabled( TelemetryScenario scenario )
    {
        // IsEnabled collapses the reporting action to a boolean, which is only meaningful for scenarios that have no
        // ASK state. Exception and Performance can be ASK (ReportingAction.Default = capture + ask, no auto-send), where
        // a boolean would be ambiguous, so callers must use GetEffectiveReportingAction for those. See #1674, #1701.
        switch ( scenario )
        {
            case TelemetryScenario.Exception:
            case TelemetryScenario.Performance:
                throw new ArgumentOutOfRangeException(
                    nameof(scenario),
                    scenario,
                    $"The '{scenario}' scenario can be in an ASK state; use {nameof(this.GetEffectiveReportingAction)} instead." );
        }

        return this.GetEffectiveReportingAction( scenario ) != ReportingAction.No;
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

    public void ResetReportedIssues() => this._configurationManager.Update<TelemetryConfiguration>( c => c with { Issues = c.Issues.Clear() } );

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