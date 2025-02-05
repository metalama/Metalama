// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.Utilities;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Metalama.Backstage.Licensing.Registration;

internal class LicenseRegistrationService : ILicenseRegistrationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserDeviceDetectionService _userDeviceDetectionService;

    public LicenseRegistrationService( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
        this._dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._userDeviceDetectionService = serviceProvider.GetRequiredBackstageService<IUserDeviceDetectionService>();
        
        // We intentionally omit to unsubscribe from the event because this service has generally the same lifetime as the application
        // and is never disposed of.
        serviceProvider.GetRequiredBackstageService<IConfigurationManager>().ConfigurationFileChanged += this.OnConfigurationChanged;
    }

    private void OnConfigurationChanged( ConfigurationFile obj )
    {
        if ( obj is LicensingConfiguration )
        {
            this.OnPropertyChanged( nameof(this.RegisteredLicense) );
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
    /// Attempts to register an unsigned Metalama Free license.
    /// </summary>
    /// <returns>
    /// A value indicating whether the license has been registered.
    /// Success is indicated when a new Metalama Free license is registered
    /// as well as when an existing Metalama Free license is registered already.
    /// </returns>
    public bool TryRegisterFreeEdition( [NotNullWhen( false )] out string? errorMessage )
    {
        void TraceFailure( string message )
        {
            this._logger.Trace?.Log( $"Failed to register Metalama Free license: {message}" );
        }

        if ( !this.RequireAttendedSession( out errorMessage ) )
        {
            return false;
        }

        this._logger.Trace?.Log( "Registering Metalama Free license." );

        try
        {
            var userStorage = LicensingConfigurationModel.Create( this._serviceProvider );

            if ( userStorage.LicenseProperties is { Product: LicensedProduct.MetalamaFree } )
            {
                TraceFailure( "A Metalama Free license is registered already." );

                errorMessage = null;

                return true;
            }

            var factory = new UnsignedLicenseFactory( this._serviceProvider );
            var (licenseKey, data) = factory.CreateFreeLicense();

            userStorage.SetLicense( licenseKey, data );
        }
        catch ( Exception e )
        {
            TraceFailure( e.ToString() );

            errorMessage = "An unexpected exception was thrown: " + e.Message;

            return false;
        }

        errorMessage = null;

        return true;
    }

    public bool TryRegisterTrialEdition( [NotNullWhen( false )] out string? errorMessage )
    {
        if ( !this.RequireAttendedSession( out errorMessage ) )
        {
            return false;
        }

        this._logger.Trace?.Log( "Attempting to register an evaluation license." );

        using ( MutexHelper.WithGlobalLock( $"Evaluation_{Environment.UserName}" ) )
        {
            var configuration = LicensingConfigurationModel.Create( this._serviceProvider );

            // If the configuration file contains an evaluation license created today, this may be a race condition with
            // another process also trying to activate the evaluation mode. In this case, we just pretend we have succeeded.
            if ( configuration.IsEvaluationActive )
            {
                this._logger.Trace?.Log( "Another process started the evaluation period today." );

                errorMessage = null;

                return true;
            }

            if ( !configuration.CanStartEvaluation )
            {
                errorMessage = $"You cannot start a new trial period until {configuration.NextEvaluationStartDate}.";
                this._logger.Warning?.Log( errorMessage );

                return false;
            }

            var factory = new UnsignedLicenseFactory( this._serviceProvider );
            var (licenseKey, data) = factory.CreateEvaluationLicense();

            configuration.SetLicense( licenseKey, data );
            configuration.LastEvaluationStartDate = this._dateTimeProvider.UtcNow;
        }

        errorMessage = null;

        return true;
    }

    public bool TryRegisterLicense( string licenseString, [NotNullWhen( false )] out string? errorMessage )
    {
        if ( !this.RequireAttendedSession( out errorMessage ) )
        {
            return false;
        }

        var factory = new LicenseFactory( this._serviceProvider );

        if ( !factory.TryCreate( licenseString, out var license, out errorMessage )
             || !license.TryGetProperties( out var data, out errorMessage ) )
        {
            return false;
        }

        var storage = LicensingConfigurationModel.Create( this._serviceProvider );
        storage.SetLicense( licenseString, data );

        return true;
    }

    public bool CanRegisterTrialEdition
    {
        get
        {
            var configuration = LicensingConfigurationModel.Create( this._serviceProvider );

            return configuration.CanStartEvaluation;
        }
    }

    public bool TryRemoveCurrentLicense( [NotNullWhen( true )] out string? licenseString )
    {
        var licenseStorage = LicensingConfigurationModel.Create( this._serviceProvider );

        if ( string.IsNullOrWhiteSpace( licenseStorage.LicenseString ) )
        {
            licenseString = null;

            return false;
        }
        else
        {
            licenseString = licenseStorage.LicenseString!;
            licenseStorage.RemoveLicense();

            return true;
        }
    }

    public LicenseProperties? RegisteredLicense
    {
        get
        {
            var licenseStorage = LicensingConfigurationModel.Create( this._serviceProvider );

            return licenseStorage.LicenseProperties;
        }
    }

    public bool TryValidateLicenseKey( string licenseKey, [NotNullWhen( false )] out string? errorMessage )
    {
        var factory = new LicenseFactory( this._serviceProvider );

        return factory.TryCreate( licenseKey, out var license, out errorMessage )
               && license.TryGetProperties( out var _, out errorMessage );
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged( [CallerMemberName] string? propertyName = null )
    {
        this.PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
    }
}