// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Audit;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Licensing.Licenses;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Backstage.Licensing.Consumption;

internal class LicenseConsumer : ILicenseConsumer
{
    private readonly ILogger _logger;
    private readonly LicenseConsumptionData? _license;
    private readonly NamespaceLicenseInfo? _licensedNamespace;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILicenseAuditManager? _licenseAuditManager;

    private DateTime _lastAuditTime = DateTime.MinValue;

    private LicenseConsumer( IServiceProvider services, LicenseConsumptionData? license, NamespaceLicenseInfo? licensedNamespace )
    {
        this._license = license;
        this._licensedNamespace = licensedNamespace;
        this._logger = services.GetLoggerFactory().Licensing();
        this._dateTimeProvider = services.GetRequiredBackstageService<IDateTimeProvider>();
        this._licenseAuditManager = services.GetBackstageService<ILicenseAuditManager>();
        this._backgroundTasksService = services.GetRequiredBackstageService<BackstageBackgroundTasksService>();
    }

    public static ILicenseConsumer Create(
        IServiceProvider services,
        IReadOnlyList<ILicenseSource> licenseSources,
        out ImmutableArray<LicensingMessage> messages )
    {
        var messagesBuilder = ImmutableArray.CreateBuilder<LicensingMessage>();

        var logger = services.GetLoggerFactory().Licensing();

        NamespaceLicenseInfo? licensedNamespace = null;

        LicenseConsumptionData? licenseConsumptionData = null;

        foreach ( var source in licenseSources.OrderBy( s => s.Priority ) )
        {
            var license = source.GetLicense( ReportMessage );

            if ( license == null )
            {
                logger.Trace?.Log( $"'{source.GetType().Name}' license source provided no license." );

                continue;
            }

            if ( !license.TryGetLicenseConsumptionData( out licenseConsumptionData, out var errorMessage ) )
            {
                _ = license.TryGetProperties( out var registrationData, out _ );
                var message = registrationData == null ? "A license" : $"The {registrationData.Description}";
                message += $" {errorMessage}.";

                if ( registrationData is { IsSelfCreated: false } )
                {
                    message += $" License key ID: '{registrationData.LicenseId}'.";
                }

                if ( source.GetType() != typeof(UserProfileLicenseSource) )
                {
                    message += $" The license key originates from {source.Description}.";
                }

                ReportMessage( new LicensingMessage( message ) );

                continue;
            }

            licensedNamespace = string.IsNullOrEmpty( licenseConsumptionData.LicensedNamespace )
                ? null
                : new NamespaceLicenseInfo( licenseConsumptionData.LicensedNamespace! );

            break;
        }

        messages = messagesBuilder.ToImmutable();

        return new LicenseConsumer( services, licenseConsumptionData, licensedNamespace );

        void ReportMessage( LicensingMessage message )
        {
            messagesBuilder.Add( message );
            logger.Warning?.Log( message.Text );
        }
    }

    /// <inheritdoc />
    public bool CanConsume( LicenseRequirement requirement, string? consumerProjectName = null )
    {
        if ( this._license == null )
        {
            this._logger.Warning?.Log( "No license provided." );

            return false;
        }

        this.AuditIfNecessary();

        // Redistributable licenses allow building any namespace. Their namespace field limits the redistribution only.
        // Ie., the namespace of redistributed aspects built with the redistribution license key.
        if ( !this.IsRedistributionLicense
             && !string.IsNullOrEmpty( consumerProjectName )
             && this._licensedNamespace != null
             && !this._licensedNamespace.AllowsNamespace( consumerProjectName ) )
        {
            this._logger.Warning?.Log(
                $"Project '{consumerProjectName}' is not licensed. Your license is limited to project names beginning with '{this._licensedNamespace.AllowedNamespace}'." );

            return false;
        }

        if ( !requirement.IsFulfilledBy( this._license ) )
        {
            this._logger.Warning?.Log( $"License requirement '{requirement}' is not licensed." );

            return false;
        }

        return true;
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

    public bool IsTrialLicense => this._license?.LicenseType == LicenseType.Evaluation;

    public bool IsRedistributionLicense => this._license?.IsRedistributable == true;

    public string? LicenseString => this._license?.LicenseString;
}