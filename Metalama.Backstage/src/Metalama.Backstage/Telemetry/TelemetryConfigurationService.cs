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
    public const string OptOutEnvironmentVariable = TelemetryConfiguration.OptOutEnvironmentVariableName;

    private readonly IConfigurationManager _configurationManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly RandomNumberGenerator _randomNumberGenerator;

    private TelemetryDisabledReason _globallyDisabledReason;
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

    private long _matomoSalt;
    private long _usageTrackingSalt;
    private long _exceptionReportingSalt;
    private long _licenseAuditSalt;

    [Obsolete( "Use GetSalt(TelemetrySaltKind.Matomo) instead." )]
    public long MatomoSalt => this._matomoSalt;

    [Obsolete( "Use GetSalt(TelemetrySaltKind.UsageTracking) instead." )]
    public long UsageTrackingSalt => this._usageTrackingSalt;

    [Obsolete( "Use GetSalt(TelemetrySaltKind.ExceptionReport) instead." )]
    public long ExceptionReportingSalt => this._exceptionReportingSalt;

    [Obsolete( "Use GetSalt(TelemetrySaltKind.LicenseAudit) instead." )]
    public long LicenseAuditSalt => this._licenseAuditSalt;

    public long GetSalt( TelemetrySaltKind kind )
        => kind switch
        {
            TelemetrySaltKind.ExceptionReport => this._exceptionReportingSalt,
            TelemetrySaltKind.UsageTracking => this._usageTrackingSalt,
            TelemetrySaltKind.LicenseAudit => this._licenseAuditSalt,
            TelemetrySaltKind.Matomo => this._matomoSalt,
            _ => throw new ArgumentOutOfRangeException( nameof(kind), kind, null )
        };

    private void OnConfigurationChanged( ConfigurationFile configuration )
    {
        if ( configuration is TelemetryConfiguration telemetryConfiguration )
        {
            this.ReadConfiguration( telemetryConfiguration );
        }
    }

    private TelemetryDisabledReason ComputeGlobalTelemetryDisabledReason()
    {
        // Check if the current application supports telemetry.
        var applicationInfo = this._serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
        var isApplicationTelemetryEnabled = applicationInfo.IsTelemetryEnabled;

        if ( !isApplicationTelemetryEnabled )
        {
            this._logger.Trace?.Log( $"Telemetry is disabled for '{applicationInfo.Name} {applicationInfo.PackageVersion}'." );

            return TelemetryDisabledReason.UnsupportedApplication;
        }

        // Check if the current process is unattended.
        if ( applicationInfo.IsUnattendedProcess( this._serviceProvider.GetLoggerFactory() ) )
        {
            this._logger.Trace?.Log( $"Telemetry is disabled because the current process is unattended." );

            return TelemetryDisabledReason.UnattendedProcess;
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

            return TelemetryDisabledReason.EnvironmentVariableOptOut;
        }

        return TelemetryDisabledReason.None;
    }

    // Caches the salts and device identifier read from the configuration. The per-scenario enablement is NOT cached: it
    // is computed on demand by GetEffectiveReportingAction, which reads the (cached) configuration and applies the
    // process-level gate. See #1674, #1701.
    private void ReadConfiguration( TelemetryConfiguration configuration )
    {
        // We should not have null values here because EnsureActivated sets it.
        this._matomoSalt = configuration.MatomoSalt ?? 0;
        this._usageTrackingSalt = configuration.UsageTrackingSalt ?? 0;
        this._exceptionReportingSalt = configuration.ExceptionReportingSalt ?? 0;
        this._licenseAuditSalt = configuration.LicenseAuditSalt ?? 0;
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
            this._globallyDisabledReason = this.ComputeGlobalTelemetryDisabledReason();

            // If telemetry was already activated in a previous session, keep the monthly rotation / salt back-fill up to
            // date. This never creates a DeviceId for a not-yet-activated configuration (it is a no-op in that case).
            this.RotateDeviceIdAndSaltIfActivated();

            this.ReadConfiguration( this._configurationManager.Get<TelemetryConfiguration>() );
            this._initialized = true;
        }
    }

    private void EnsureInitialized()
    {
        if ( !this._initialized )
        {
            throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Ensures that the <see cref="DeviceId"/>, <see cref="ExceptionReportingSalt"/>, <see cref="MatomoSalt"/>, <see cref="UsageTrackingSalt"/>
    /// properties are property initialized.
    /// </summary>
    public void EnsureActivated()
    {
        lock ( this._activationSync )
        {
            var activatedNow = this._configurationManager.UpdateIf<TelemetryConfiguration>(
                c => c.DeviceId == null,
                c => c with
                {
                    // We only write the activation artifacts here (the device identifier, the salts and the upload
                    // timing). We deliberately do NOT set the per-channel reporting actions: their record defaults
                    // already express the intended policy — usage is opt-out (ReportingAction.Default is treated as
                    // enabled, see ReadConfiguration) and the exception/performance channels are review-first
                    // (ReportingAction.Default, see #1674). Setting them here would clobber any choice the user made
                    // through SetStatus before telemetry was activated (activation is now lazy, so it can run after
                    // an in-product opt-in/opt-out). See #1701, #1674.
                    DeviceId = this._randomNumberGenerator.NextGuid(),
                    MatomoSalt = this.NextSalt(),
                    UsageTrackingSalt = this.NextSalt(),
                    ExceptionReportingSalt = this.NextSalt(),
                    LicenseAuditSalt = this.NextSalt(),
                    LastSaltChangeTime = this._dateTimeProvider.UtcNow,

                    // Make sure we don't upload telemetry data on the first second of use.
                    // Since first-time users are likely not to use the software for more than a few minutes,
                    // configure so that we will upload data in 15 minutes.
                    LastUploadTime = this._dateTimeProvider.UtcNow.AddDays( -1 ).AddMinutes( 15 )
                } );

            if ( activatedNow )
            {
                this._logger.Trace?.Log( "Telemetry was activated: a device identifier and salts were created." );
            }

            this.RotateDeviceIdAndSaltIfActivated();
            this.ReadConfiguration( this._configurationManager.Get<TelemetryConfiguration>() );
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
            c => c with
            {
                MatomoSalt = this.NextSalt(),
                UsageTrackingSalt = this.NextSalt(),
                ExceptionReportingSalt = this.NextSalt(),
                LicenseAuditSalt = this.NextSalt(),
                DeviceId = this._randomNumberGenerator.NextGuid(),
                LastSaltChangeTime = this._dateTimeProvider.UtcNow
            } );

        if ( rotated )
        {
            this._logger.Trace?.Log( "Telemetry IDs and salts were rotated." );
        }

        // Back-fill the first-party diagnostic salts for configurations created before they were introduced,
        // without rotating the Matomo Salt or the DeviceId (which would reset Matomo visitor continuity). The
        // back-filled salts are distinct from each other and from any salt already present. See #1668.
        this._configurationManager.UpdateIf<TelemetryConfiguration>(
            c => c.DeviceId != null && (c.UsageTrackingSalt == null || c.ExceptionReportingSalt == null || c.LicenseAuditSalt == null),
            c => c with
            {
                UsageTrackingSalt = c.UsageTrackingSalt ?? this.NextSalt(),
                ExceptionReportingSalt = c.ExceptionReportingSalt ?? this.NextSalt(),
                LicenseAuditSalt = c.LicenseAuditSalt ?? this.NextSalt()
            } );
    }

    public void SetStatus( bool enabled )
    {
        var reportAction = enabled switch
        {
            true => TelemetryConsent.Yes,
            false => TelemetryConsent.No
        };

        this.SetConsent( reportAction );
    }

    public void SetStatus( TelemetryScenario scenario, bool enabled )
    {
        var reportAction = enabled ? TelemetryConsent.Yes : TelemetryConsent.No;

        this.SetConsent( scenario, reportAction );
    }

    public void SetConsent( TelemetryConsent telemetryConsent )
    {
        this.EnsureInitialized();

        this._configurationManager.Update<TelemetryConfiguration>(
            c => c with { UsageConsent = telemetryConsent, ExceptionConsent = telemetryConsent, PerformanceProblemConsent = telemetryConsent } );
    }

    public void SetConsent( TelemetryScenario scenario, TelemetryConsent action )
    {
        this.EnsureInitialized();

        this._configurationManager.Update<TelemetryConfiguration>(
            c => scenario switch
            {
                TelemetryScenario.Usage => c with { UsageConsent = action },
                TelemetryScenario.Exception => c with { ExceptionConsent = action },
                TelemetryScenario.Performance => c with { PerformanceProblemConsent = action },
                _ => throw new ArgumentOutOfRangeException( nameof(scenario), scenario, "This telemetry scenario cannot be configured." )
            } );
    }

    public bool CompareExchangeConsent( TelemetryScenario scenario, TelemetryConsent newAction, TelemetryConsent oldAction )
    {
        this.EnsureInitialized();

        return this._configurationManager.UpdateIf<TelemetryConfiguration>(
            c =>
            {
                var oldStatus = scenario switch
                {
                    TelemetryScenario.Usage => c.UsageConsent,
                    TelemetryScenario.Exception => c.ExceptionConsent,
                    TelemetryScenario.Performance => c.PerformanceProblemConsent,
                    _ => throw new ArgumentOutOfRangeException( nameof(scenario), scenario, "This telemetry scenario cannot be configured." )
                };

                return oldStatus == oldAction;
            },
            c => scenario switch
            {
                TelemetryScenario.Usage => c with { UsageConsent = newAction },
                TelemetryScenario.Exception => c with { ExceptionConsent = newAction },
                TelemetryScenario.Performance => c with { PerformanceProblemConsent = newAction },
                _ => throw new ArgumentOutOfRangeException( nameof(scenario), scenario, "This telemetry scenario cannot be configured." )
            } );
    }

    public Guid DeviceId { get; private set; }

    public bool IsActivated => this.DeviceId != Guid.Empty;

    public bool IsGloballyEnabled
    {
        get
        {
            this.EnsureInitialized();

            return this._globallyDisabledReason == TelemetryDisabledReason.None;
        }
    }

    public TelemetryConsent GetEffectiveConsent( TelemetryScenario scenario ) => this.GetEffectiveConsentAndReason( scenario ).Consent;

    public (TelemetryConsent Consent, TelemetryDisabledReason Reason) GetEffectiveConsentAndReason( TelemetryScenario scenario )

    {
        // The process-level gate overrides the configured per-category action: when telemetry is disabled for the
        // process (unattended, opt-out environment variable, or unsupported application), every scenario is effectively
        // No. See #1701.
        if ( !this.IsGloballyEnabled )
        {
            return (TelemetryConsent.No, this._globallyDisabledReason);
        }

        var configuration = this._configurationManager.Get<TelemetryConfiguration>();

        var consent = scenario switch
        {
            // The news feed counts as usage telemetry for opt-out purposes: an in-product opt-out (SetStatus(false))
            // must stop the RSS fetch, just like the opt-out environment variable. See #1670.
            TelemetryScenario.Usage => configuration.UsageConsent,
            TelemetryScenario.Exception => configuration.ExceptionConsent,
            TelemetryScenario.Performance => configuration.PerformanceProblemConsent,
            _ => throw new ArgumentOutOfRangeException( nameof(scenario), scenario, null )
        };

        if ( consent == TelemetryConsent.No )
        {
            return (TelemetryConsent.No, TelemetryDisabledReason.UserOptOut);
        }
        else
        {
            return (consent, TelemetryDisabledReason.None);
        }
    }

    public void ResetDeviceId()
    {
        this.EnsureInitialized();

        this._configurationManager.Update<TelemetryConfiguration>(
            c => c with
            {
                DeviceId = this._randomNumberGenerator.NextGuid(),
                MatomoSalt = this.NextSalt(),
                UsageTrackingSalt = this.NextSalt(),
                ExceptionReportingSalt = this.NextSalt(),
                LicenseAuditSalt = this.NextSalt(),
                LastSaltChangeTime = this._dateTimeProvider.UtcNow
            } );
    }

    public void ResetReportedIssues() => this._configurationManager.Update<TelemetryConfiguration>( c => c with { Issues = c.Issues.Clear() } );

    // Returns a cryptographically-secure salt.
    private long NextSalt() => this._randomNumberGenerator.NextCryptographicInt64();
}