// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Audit;
using Metalama.Backstage.Licensing.Consumption.Sources;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Backstage.Licensing.Consumption;

internal sealed class LicenseConsumer : ILicenseConsumer
{
    public ImmutableArray<LicensingMessage> Messages { get; }

    private readonly ILogger _logger;
    private readonly LicenseConsumptionProperties? _license;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILicenseAuditManager? _licenseAuditManager;

    private DateTime _lastAuditTime = DateTime.MinValue;

    private LicenseConsumer(
        IServiceProvider services,
        LicenseConsumptionProperties? license,
        ImmutableArray<LicensingMessage> messages )
    {
        this.Messages = messages;
        this._license = license;
        this._logger = services.GetLoggerFactory().Licensing();
        this._dateTimeProvider = services.GetRequiredBackstageService<IDateTimeProvider>();
        this._licenseAuditManager = services.GetBackstageService<ILicenseAuditManager>();
        this._backgroundTasksService = services.GetRequiredBackstageService<BackstageBackgroundTasksService>();
    }

    public static ILicenseConsumer Create(
        LicenseConsumptionOptions options,
        IServiceProvider services,
        IEnumerable<ILicenseSource> licenseSources )
    {
        var messagesBuilder = ImmutableArray.CreateBuilder<LicensingMessage>();

        var logger = services.GetLoggerFactory().Licensing();

        LicenseConsumptionProperties? licenseConsumptionData = null;

        var licenses = licenseSources.OrderBy( s => s.Priority ).SelectMany( s => s.GetLicenses( ReportMessage ).Select( l => (License: l, Source: s) ) );

        foreach ( var license in licenses )
        {
            if ( !license.License.TryGetConsumptionProperties( options, out licenseConsumptionData, out var errorMessage ) )
            {
                _ = license.License.TryGetRegistrationProperties( out var registrationData, out _ );
                var message = registrationData == null ? "A license" : $"The {registrationData.Description}";
                message += $" {errorMessage}.";

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

            break;
        }

        return new LicenseConsumer( services, licenseConsumptionData, messagesBuilder.ToImmutable() );

        void ReportMessage( LicensingMessage message )
        {
            messagesBuilder.Add( message );
            logger.Warning?.Log( message.Text );
        }
    }

    /// <inheritdoc />
    public bool TryConsume( Predicate<LicenseConsumptionProperties> predicate )
    {
        if ( this._license == null )
        {
            this._logger.Warning?.Log( "No license provided." );

            return false;
        }

        if ( predicate( this._license ) )
        {
            this.AuditIfNecessary();

            return true;
        }
        else
        {
            return false;
        }
    }

    private void AuditIfNecessary()
    {
        // Audit the use of the license once per day (more time checks are performed by the license audit manager).
        if ( this._license != null && this._lastAuditTime.AddDays( 1 ) < this._dateTimeProvider.UtcNow )
        {
            this._lastAuditTime = this._dateTimeProvider.UtcNow;

            if ( this._licenseAuditManager != null )
            {
                this._backgroundTasksService.Enqueue( () => this._licenseAuditManager.ReportLicense( this._license ) );
            }
            else
            {
                this._logger.Warning?.Log( $"License audit is skipped because there is no {nameof(ILicenseAuditManager)}." );
            }
        }
    }
}