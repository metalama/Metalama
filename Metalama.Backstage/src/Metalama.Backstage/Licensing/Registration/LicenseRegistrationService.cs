// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.UserInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Metalama.Backstage.Licensing.Registration;

internal sealed class LicenseRegistrationService : ILicenseRegistrationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserDeviceDetectionService _userDeviceDetectionService;
    private readonly IConfigurationManager _configurationManager;

    public LicenseRegistrationService( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
        this._dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._userDeviceDetectionService = serviceProvider.GetRequiredBackstageService<IUserDeviceDetectionService>();
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();

        // We intentionally omit to unsubscribe from the event because this service has generally the same lifetime as the application
        // and is never disposed of.
        serviceProvider.GetRequiredBackstageService<IConfigurationManager>().ConfigurationFileChanged += this.OnConfigurationChanged;
    }

    private void OnConfigurationChanged( ConfigurationFile obj )
    {
        if ( obj is LicensingConfiguration )
        {
            this.OnPropertyChanged( nameof(this.RegisteredLicenses) );
            this.OnPropertyChanged( nameof(this.CanRegisterTrialEdition) );
        }
    }

    private bool RequireAttendedSession( [NotNullWhen( false )] out string? errorMessage )
    {
        if ( !this._userDeviceDetectionService.IsInteractiveDevice )
        {
            errorMessage = "This command must be executed from an interactive session.";

            return false;
        }
        else
        {
            errorMessage = null;

            return true;
        }
    }

    /// <summary>
    /// Attempts to register an unsigned Metalama Community license.
    /// </summary>
    /// <returns>
    /// A value indicating whether the license has been registered.
    /// Success is indicated when a new Metalama Community license is registered
    /// as well as when an existing Metalama Community license is registered already.
    /// </returns>
    public LicenseRegistrationResult RegisterCommunityEdition( CommunityLicenseReason reason )
    {
        if ( !this.RequireAttendedSession( out var errorMessage ) )
        {
            return LicenseRegistrationResult.Failure( errorMessage );
        }

        if ( reason == CommunityLicenseReason.None )
        {
            throw new ArgumentOutOfRangeException( nameof(reason), reason, "The community license reason is invalid." );
        }

        this._logger.Trace?.Log( "Registering Metalama Community." );

        var factory = new UnsignedLicenseFactory( this._serviceProvider );
        var communityLicense = factory.CreateCommunityLicense();

        if ( !this._configurationManager.Update<LicensingConfiguration>(
                config => config.SetLicense( communityLicense ) with { CommunityLicenseReason = reason } ) )
        {
            return LicenseRegistrationResult.Failure( "Metalama Community is already registered." );
        }

        return LicenseRegistrationResult.Success( communityLicense );
    }

    [Obsolete]
    public LicenseRegistrationResult RegisterLegacyFreeEdition()
    {
        if ( !this.RequireAttendedSession( out var errorMessage ) )
        {
            return LicenseRegistrationResult.Failure( errorMessage );
        }

        this._logger.Trace?.Log( "Registering Metalama Free." );

        var factory = new UnsignedLicenseFactory( this._serviceProvider );
        var communityLicense = factory.CreateLegacyFreeLicense();

        this._configurationManager.Update<LicensingConfiguration>(
            config =>
            {
                return config.SetLicense( communityLicense );
            } );

        return LicenseRegistrationResult.Success( communityLicense );
    }

    public LicenseRegistrationResult RegisterTrialEdition()
    {
        if ( !this.RequireAttendedSession( out var errorMessage ) )
        {
            return LicenseRegistrationResult.Failure( errorMessage );
        }

        this._logger.Trace?.Log( "Attempting to register an evaluation license." );

        if ( !this.CanRegisterTrialEditionCore( out errorMessage ) )
        {
            return LicenseRegistrationResult.Failure( errorMessage );
        }

        var factory = new UnsignedLicenseFactory( this._serviceProvider );
        var evaluationLicense = factory.CreateEvaluationLicense();

        this._configurationManager.Update<LicensingConfiguration>(
            config => config.SetLicense( evaluationLicense ) with { LastEvaluationStartDate = this._dateTimeProvider.UtcNow } );

        return LicenseRegistrationResult.Success( evaluationLicense );
    }

    private bool CanRegisterTrialEditionCore( [NotNullWhen( false )] out string? errorMessage )
    {
        var currentConfiguration = this._configurationManager.Get<LicensingConfiguration>();

        if ( currentConfiguration.GetRegisteredLicenses()
            .Any( l => l is { LicenseType: LicenseType.Evaluation } && l.ValidTo >= this._dateTimeProvider.UtcNow ) )
        {
            errorMessage = "The evaluation license is already active.";

            return false;
        }

        var lastEvaluationStartDate = currentConfiguration.LastEvaluationStartDate ?? DateTime.MinValue;
        var nextEvaluationStartDate = lastEvaluationStartDate + LicensingConstants.NoEvaluationPeriod + LicensingConstants.EvaluationPeriod;

        if ( nextEvaluationStartDate > this._dateTimeProvider.UtcNow )
        {
            errorMessage = $"You cannot start a new trial period until {nextEvaluationStartDate}.";
            this._logger.Warning?.Log( errorMessage );

            return false;
        }

        errorMessage = null;

        return true;
    }

    public LicenseRegistrationResult RegisterLicense( string licenseString )
    {
        return this.RegisterLicenseCore( licenseString, false );
    }

    public LicenseRegistrationResult ValidateLicenseKey( string licenseKey )
    {
        return this.RegisterLicenseCore( licenseKey, true );
    }

    public LicenseRegistrationResult ParseLicenseKey( string licenseKey )
    {
        if ( !this.RequireAttendedSession( out var errorMessage ) )
        {
            return LicenseRegistrationResult.Failure( errorMessage );
        }

        var factory = new LicenseFactory( this._serviceProvider );

        if ( !factory.TryCreate( licenseKey, out var license, out errorMessage )
             || !license.TryGetRegistrationProperties( out var licenseProperties, out errorMessage ) )
        {
            return LicenseRegistrationResult.Failure( errorMessage );
        }

        return LicenseRegistrationResult.Success( licenseProperties );
    }

    private LicenseRegistrationResult RegisterLicenseCore( string licenseString, bool dry )
    {
        if ( !this.RequireAttendedSession( out var errorMessage ) )
        {
            return LicenseRegistrationResult.Failure( errorMessage );
        }

        var factory = new LicenseFactory( this._serviceProvider );

        if ( !factory.TryCreate( licenseString, out var license, out errorMessage )
             || !license.TryGetRegistrationProperties( out var properties, out errorMessage ) )
        {
            return LicenseRegistrationResult.Failure( errorMessage );
        }

        if ( !license.CanBeRegistered( out errorMessage ) )
        {
            return LicenseRegistrationResult.Failure( errorMessage );
        }

        if ( !dry )
        {
            this._configurationManager.Update<LicensingConfiguration>( config => config.SetLicense( properties ) );
        }

        return LicenseRegistrationResult.Success( properties );
    }

    public bool CanRegisterTrialEdition => this.CanRegisterTrialEditionCore( out _ );

    public void RemoveLicenses()
    {
        this._configurationManager.Update<LicensingConfiguration>( config => config.RemoveAllLicenses() );
    }

    public IEnumerable<LicenseRegistrationProperties> RegisteredLicenses
        => this._configurationManager.Get<LicensingConfiguration>().GetRegisteredLicenses().Select( x => x.ToLicenseRegistrationProperties() );

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged( [CallerMemberName] string? propertyName = null )
    {
        this.PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
    }
}