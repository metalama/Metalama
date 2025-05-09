// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.UserInterface;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace Metalama.Backstage.Licensing.Consumption;

internal sealed class LicenseConsumer : ILicenseConsumer
{
    private readonly ImmutableArray<(ILicense License, LicenseConsumptionProperties Properties)> _licenses;
    private readonly LicenseConsumptionOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger _logger;
    private readonly IApplicationInfo _applicationInfo;
    private readonly IUserInterfaceService? _userInterfaceService;

    private LicenseConsumer(
        IServiceProvider services,
        ImmutableArray<(ILicense License, LicenseConsumptionProperties Properties)> licenses,
        LicenseConsumptionOptions options )
    {
        this._licenses = licenses;
        this._options = options;
        this._logger = services.GetLoggerFactory().Licensing();
        this._dateTimeProvider = services.GetRequiredBackstageService<IDateTimeProvider>();
        this._applicationInfo = services.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
        this._userInterfaceService = services.GetBackstageService<IUserInterfaceService>();
    }

    public static ILicenseConsumer Create(
        LicenseConsumptionOptions options,
        IServiceProvider services,
        IEnumerable<ILicenseSource> licenseSources,
        Action<LicensingMessage>? reportMessage = null )
    {
        var logger = services.GetLoggerFactory().Licensing();

        // Gather valid licenses.
        var licenses = licenseSources.OrderBy( s => s.Priority ).SelectMany( s => s.GetLicenses( ReportMessage ).Select( l => (License: l, Source: s) ) );

        var validLicenses = ImmutableArray.CreateBuilder<(ILicense License, LicenseConsumptionProperties Properties)>();

        foreach ( var license in licenses )
        {
            if ( !license.License.TryGetConsumptionProperties( options, out var licenseConsumptionData, out var errorMessage ) )
            {
                _ = license.License.TryGetRegistrationProperties( out var registrationData, out _ );

                var message =
                    $"Cannot use the license '{registrationData?.LicenseId?.ToString( CultureInfo.InvariantCulture ) ?? registrationData?.Description}': {errorMessage}"
                        .TrimEnd( '.' ) + ".";

                if ( license.Source.GetType() != typeof(UserProfileLicenseSource) )
                {
                    message += $" The license key originates from {license.Source.Description}.";
                }

                ReportMessage( new LicensingMessage( message ) );

                continue;
            }

            validLicenses.Add( (license.License, licenseConsumptionData) );
        }

        // Return the LicenseConsumer.
        return new LicenseConsumer( services, validLicenses.ToImmutableArray(), options );

        void ReportMessage( LicensingMessage message )
        {
            reportMessage?.Invoke( message );
            logger.Warning?.Log( message.Text );
        }
    }

    /// <inheritdoc />
    public bool TryConsume( LicenseRequirement requirement, Action<LicensingMessage>? reportMessage, bool showsToastNotification )
    {
        this._logger.Trace?.Log( $"TryConsume({{{requirement}}}" );

        foreach ( var license in this._licenses )
        {
            // Check project-bound license keys.
            if ( !string.IsNullOrEmpty( license.Properties.LicensedNamespace )
                 && (string.IsNullOrEmpty( this._options.ProjectName ) || !this._options.ProjectName!.StartsWith(
                     license.Properties.LicensedNamespace!,
                     StringComparison.OrdinalIgnoreCase )) )
            {
                reportMessage?.Invoke(
                    new LicensingMessage(
                        $"The license key '{license.Properties.DisplayName}' is bound to the " +
                        $"'{license.Properties.LicensedNamespace}' namespace, but current project name is '{this._options.ProjectName}'." ) );

                this._logger.Warning?.Log(
                    $"TryConsume({{{requirement}}}: license key '{license.Properties.DisplayName}' ignored because it is bound to the namespace" +
                    $" '{license.Properties.LicensedNamespace}' it does not match the current project name '{this._options.ProjectName}'." );

                continue;
            }

            // Check eligibility.
            if ( requirement.IsEligible(
                    new LicenseConsumptionContext( license.Properties, this._applicationInfo, this._dateTimeProvider.UtcNow, this._logger ) ) )
            {
                this._logger.Trace?.Log( $"TryConsume({{{requirement}}}: '{license.Properties.DisplayName}' is eligible." );

                license.License.ReportUse();

                return true;
            }
            else
            {
                this._logger.Trace?.Log( $"TryConsume({{{requirement}}}: '{license.Properties.DisplayName}' is not eligible." );
            }
        }

        this._logger.Warning?.Log( $"TryConsume({{{requirement}}}: no eligible license found." );

        var messageText =
            $"The component '{requirement.ComponentNameWithServicingPhase}' is not licensed. It requires one of the following products: "
            + string.Join( ", ", requirement.EligibleProductNames )
            + ".";

        if ( this._licenses.IsEmpty )
        {
            messageText += " Could not find any valid registered license.";
        }
        else
        {
            messageText +=
                $" {this._licenses.Length} license keys were considered, but none was eligible: {string.Join( "; ", this._licenses.Select( x => x.Properties.LicenseString ) )}.";
        }

        // Report a licensing message (this is typically reported as a compiler diagnostic).
        reportMessage?.Invoke( new LicensingMessage( messageText ) { IsError = true } );

        // Show a toast notification, unless the application provides its own UI.
        if ( showsToastNotification )
        {
            this._userInterfaceService?.ShowToastNotification(
                new ToastNotification(
                    ToastNotificationKinds.RequiresLicense,
                    "Metalama Professional",
                    messageText + "Open to start a trial or register a license key." ) );
        }

        return false;
    }
}