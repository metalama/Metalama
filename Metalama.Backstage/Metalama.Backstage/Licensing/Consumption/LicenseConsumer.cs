// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Licensing.Licenses;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Backstage.Licensing.Consumption;

internal sealed class LicenseConsumer : ILicenseConsumer
{
    private readonly ImmutableArray<(ILicense License, LicenseConsumptionProperties Properties)> _licenses;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger _logger;
    private readonly IApplicationInfo _applicationInfo;

    private DateTime _lastAuditTime = DateTime.MinValue;

    private LicenseConsumer(
        IServiceProvider services,
        ImmutableArray<(ILicense License, LicenseConsumptionProperties Properties)> licenses )
    {
        this._licenses = licenses;
        this._logger = services.GetLoggerFactory().Licensing();
        this._dateTimeProvider = services.GetRequiredBackstageService<IDateTimeProvider>();
        this._applicationInfo = services.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
    }

    public static ILicenseConsumer Create(
        LicenseConsumptionOptions options,
        IServiceProvider services,
        IEnumerable<ILicenseSource> licenseSources,
        Action<LicensingMessage>? reportMessage = null )
    {
        var logger = services.GetLoggerFactory().Licensing();

        var licenses = licenseSources.OrderBy( s => s.Priority ).SelectMany( s => s.GetLicenses( ReportMessage ).Select( l => (License: l, Source: s) ) );

        var validLicenses = ImmutableArray.CreateBuilder<(ILicense License, LicenseConsumptionProperties Properties)>();

        foreach ( var license in licenses )
        {
            if ( !license.License.TryGetConsumptionProperties( options, out var licenseConsumptionData, out var errorMessage ) )
            {
                _ = license.License.TryGetRegistrationProperties( out var registrationData, out _ );
                var message = $"Cannot use the license '{registrationData?.Description}': {errorMessage}";

                if ( registrationData is { IsSelfCreated: false } )
                {
                    message += $" License key ID: '{registrationData.LicenseId}'.";
                }

                if ( license.Source.GetType() != typeof(UserProfileLicenseSource) )
                {
                    message += $" The license key originates from {license.Source.Description}.";
                }

                ReportMessage( new LicensingMessage( message ) );

                continue;
            }

#pragma warning disable CS0612 // Type or member is obsolete
            if ( !string.IsNullOrEmpty( licenseConsumptionData.LicensedNamespace ) )
            {
                logger.Warning?.Log( $"The license '{licenseConsumptionData.LicenseString}' has a namespace constraint, which is no longer supported." );

                continue;
            }
#pragma warning restore CS0612 // Type or member is obsolete

            validLicenses.Add( (license.License, licenseConsumptionData) );
        }

        return new LicenseConsumer( services, validLicenses.ToImmutableArray() );

        void ReportMessage( LicensingMessage message )
        {
            reportMessage?.Invoke( message );
            logger.Warning?.Log( message.Text );
        }
    }

    /// <inheritdoc />
    public bool TryConsume( LicenseRequirement requirement, Action<LicensingMessage>? reportMessage )
    {
        var mustAudit = false;

        this._logger.Trace?.Log( $"TryConsume({{{requirement}}}" );

        if ( this._lastAuditTime.AddDays( 1 ) < this._dateTimeProvider.UtcNow )
        {
            this._lastAuditTime = this._dateTimeProvider.UtcNow;
            mustAudit = true;
        }

        foreach ( var license in this._licenses )
        {
            if ( requirement.IsEligible(
                    new LicenseConsumptionContext( license.Properties, this._applicationInfo, this._dateTimeProvider.UtcNow, this._logger ) ) )
            {
                this._logger.Trace?.Log( $"TryConsume({{{requirement}}}: '{license.Properties.DisplayName}' is eligible" );

                if ( mustAudit )
                {
                    license.License.OnConsumed();
                }

                return true;
            }
            else
            {
                this._logger.Trace?.Log( $"TryConsume({{{requirement}}}: '{license.Properties.DisplayName}' is not eligible" );
            }
        }

        reportMessage?.Invoke(
            new LicensingMessage( $"The component '{requirement.ComponentName}' is not licensed: it requires {requirement.RequiredLicenseDescription}." )
            {
                IsError = true
            } );

        // TODO: We might open some UI here.

        this._logger.Warning?.Log( $"TryConsume({{{requirement}}}: no eligible license found." );

        return false;
    }
}