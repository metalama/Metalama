// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Audit;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Registration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Metalama.Backstage.Licensing.Licenses
{
    /// <summary>
    /// Represents a license serialized in a license key.
    /// </summary>
    internal sealed class License : ILicense
    {
        private readonly string _licenseKey;

        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger _logger;
        private readonly LicensingAuthority _licensingAuthority;
        private readonly IApplicationInfo _applicationInfo;
        private readonly ILicenseAuditManager? _licenseAuditManager;
        private readonly BackstageBackgroundTasksService _backgroundTasksService;

        /// <summary>
        /// Initializes a new instance of the <see cref="License"/> class.
        /// </summary>
        /// <param name="licenseKey">The license key.</param>
        /// <param name="services">Services.</param>
        internal License( string licenseKey, IServiceProvider services )
        {
            this._licenseKey = CleanLicenseKey( licenseKey );
            this._dateTimeProvider = services.GetRequiredBackstageService<IDateTimeProvider>();
            this._licensingAuthority = services.GetRequiredBackstageService<LicensingAuthority>();
            this._logger = services.GetLoggerFactory().Licensing();
            this._applicationInfo = services.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
            this._licenseAuditManager = services.GetBackstageService<ILicenseAuditManager>();
            this._backgroundTasksService = services.GetRequiredBackstageService<BackstageBackgroundTasksService>();
        }

        private static string CleanLicenseKey( string licenseKey )
        {
            var stringBuilder = new StringBuilder( licenseKey.Length );

            // Remove all spaces from the license.
            foreach ( var c in licenseKey )
            {
                if ( char.IsLetterOrDigit( c ) || c == '-' )
                {
                    stringBuilder.Append( c );
                }
            }

            return stringBuilder.ToString().ToUpperInvariant();
        }

        public bool CanBeRegistered( [MaybeNullWhen( true )] out string errorMessage )
        {
            // Validates that the key can be consumed.
            if ( !this.TryGetConsumptionProperties( LicenseConsumptionOptions.ForRegistration, out var licenseConsumptionData, out errorMessage ) )
            {
                return false;
            }

#pragma warning disable CS0612 // Type or member is obsolete
            if ( licenseConsumptionData.IsRedistributable )
            {
                errorMessage = "this is a redistribution license key";

                return false;
            }
#pragma warning restore CS0612 // Type or member is obsolete

            return true;
        }

        /// <inheritdoc />
        public bool TryGetConsumptionProperties(
            LicenseConsumptionOptions options,
            [MaybeNullWhen( false )] out LicenseConsumptionProperties licenseConsumptionProperties,
            [MaybeNullWhen( true )] out string errorMessage )
        {
            licenseConsumptionProperties = null;

            if ( !this.TryGetLicenseKeyData( out var licenseKeyData, out errorMessage ) )
            {
                return false;
            }

#pragma warning disable CS0618
            if ( licenseKeyData.LicenseType == LicenseType.Anonymous )
            {
                errorMessage = "anonymous licenses are no longer supported";

                return false;
            }
#pragma warning restore CS0618

            if ( licenseKeyData.SignatureKeyId is 0 or 1
                 && (licenseKeyData is { LicenseId: not 0 and not 22 and < 100 } || RevokedLicenseKeys.Ids.Contains( licenseKeyData.LicenseId )) )
            {
                // We use these keys to test the LicensingAuthority.
                errorMessage = "the license key has been revoked";

                return false;
            }

            if ( licenseKeyData.RequiresSignature() && !licenseKeyData.VerifySignature( this._licensingAuthority ) )
            {
                errorMessage = "the license key has an invalid signature";

                return false;
            }

            if ( licenseKeyData.ValidFrom.HasValue && licenseKeyData.ValidFrom > this._dateTimeProvider.UtcNow )
            {
                errorMessage = "the license key is not yet valid";

                return false;
            }

            if ( licenseKeyData.ValidTo.HasValue && licenseKeyData.ValidTo < this._dateTimeProvider.UtcNow )
            {
                errorMessage = "the license key has expired";

                return false;
            }

            if ( licenseKeyData.ValidTo == null && licenseKeyData.SubscriptionEndDate == null && licenseKeyData.RequiresSignature() )
            {
                errorMessage = "the license key has neither a validity end date nor a subscription end date";

                return false;
            }

            SubscriptionStatus subscriptionStatus;

            if ( licenseKeyData.SubscriptionEndDate != null )
            {
                if ( licenseKeyData.SubscriptionEndDate >= this._dateTimeProvider.UtcNow )
                {
                    subscriptionStatus = SubscriptionStatus.Active;
                }
                else if ( options.SubscriptionGracePeriod != null && licenseKeyData.SubscriptionEndDate.Value.Add( options.SubscriptionGracePeriod.Value )
                         >= this._dateTimeProvider.UtcNow )
                {
                    subscriptionStatus = SubscriptionStatus.Grace;
                }
                else
                {
                    subscriptionStatus = SubscriptionStatus.Expired;
                }
            }
            else
            {
                subscriptionStatus = SubscriptionStatus.None;
            }

            if ( !options.IgnoreSubscriptionPeriod )
            {
                if ( !this._applicationInfo.BuildDate.HasValue )
                {
                    throw new InvalidOperationException( $"Application '{this._applicationInfo.Name}' is missing build date information." );
                }

                var latestComponentMadeByPostSharp = this._applicationInfo.GetLatestComponentMadeByPostSharp();

                var endDate = licenseKeyData.SubscriptionEndDate;

                if ( endDate < latestComponentMadeByPostSharp.BuildDate )
                {
                    errorMessage =
                        $"the license key does not allow to use the licensed product '{latestComponentMadeByPostSharp.Name}' version {latestComponentMadeByPostSharp.PackageVersion} released on {latestComponentMadeByPostSharp.BuildDate:d} - only versions released before {licenseKeyData.SubscriptionEndDate:d} are allowed to use by this license";

                    return false;
                }
            }

            var licenseType = licenseKeyData.NormalizeLicenseType();
            var product = licenseKeyData.NormalizeProduct();

            switch ( product )
            {
                case LicenseProduct.MetalamaCommunity:
                case LicenseProduct.MetalamaProfessional:
                case LicenseProduct.PostSharpFramework:
                case LicenseProduct.PostSharpUltimate:
                    break;

#pragma warning disable CS0618 // Type or member is obsolete

                // No longer issued but existing keys are fully supported.
                case LicenseProduct.MetalamaUltimate:
                case LicenseProduct.MetalamaStarter:
                case LicenseProduct.MetalamaFree:
#pragma warning restore CS0618 // Type or member is obsolete
                    break;

                default:
                    errorMessage = $"the license key is for {licenseKeyData.Product} and not for Metalama";

                    return false;
            }

#pragma warning disable CS0612 // Type or member is obsolete
            if ( licenseKeyData is { IsRedistribution: true, IsLimitedByNamespace: false } )
            {
                errorMessage = "is a redistribution license, but it is not limited by a namespace";

                return false;
            }
#pragma warning restore CS0612 // Type or member is obsolete

            errorMessage = null;

#pragma warning disable CS0618 // Type or member is obsolete
            var isRedistributable = licenseType is LicenseType.OpenSourceRedistribution or LicenseType.CommercialRedistribution;
#pragma warning restore CS0618 // Type or member is obsolete

            licenseConsumptionProperties = new LicenseConsumptionProperties(
                product,
                licenseType,
                licenseKeyData.Namespace,
                $"{licenseKeyData.GetDisplayName()}, Id={licenseKeyData.LicenseUniqueId}",
                licenseKeyData.GetMinPostSharpVersion(),
                licenseKeyData.LicenseString,
                isRedistributable,
                licenseKeyData.Auditable ?? true,
                licenseKeyData.SubscriptionEndDate,
                subscriptionStatus,
                licenseKeyData.Generation );

            return true;
        }

        /// <inheritdoc />
        public bool TryGetRegistrationProperties(
            [MaybeNullWhen( false )] out LicenseRegistrationProperties licenseProperties,
            [MaybeNullWhen( true )] out string errorMessage )
        {
            if ( !this.TryGetLicenseKeyData( out var licenseKeyData, out errorMessage ) )
            {
                licenseProperties = null;

                return false;
            }

            if ( licenseKeyData.RequiresSignature() && !licenseKeyData.VerifySignature( this._licensingAuthority ) )
            {
                errorMessage = $"The license key {licenseKeyData.LicenseUniqueId} has an invalid signature.";
                this._logger.Warning?.Log( errorMessage );
                licenseProperties = null;

                return false;
            }

            licenseProperties = licenseKeyData.ToLicenseRegistrationProperties();

            return true;
        }

        public void OnConsumed()
        {
            if ( this._licenseAuditManager != null )
            {
                if ( this.TryGetConsumptionProperties( LicenseConsumptionOptions.Default, out var properties, out _ ) )
                {
                    this._backgroundTasksService.Enqueue( () => this._licenseAuditManager.ReportLicense( properties ) );
                }
            }
            else
            {
                this._logger.Warning?.Log( $"License audit is skipped because there is no {nameof(ILicenseAuditManager)}." );
            }
        }

        private bool TryGetLicenseKeyData( [MaybeNullWhen( false )] out LicenseKeyData data, [MaybeNullWhen( true )] out string errorMessage )
        {
            this._logger.Trace?.Log( $"Deserializing license '{this._licenseKey}'." );

            if ( !LicenseKeyData.TryDeserialize( this._licenseKey, out data, out errorMessage ) || !data.ValidateFields( out errorMessage ) )
            {
                errorMessage = $"Cannot parse the license key '{this._licenseKey}': {errorMessage}.";

                this._logger.Error?.Log( errorMessage );

                return false;
            }
            else
            {
                this._logger.Trace?.Log( $"Deserialized license: {data}" );

                return true;
            }
        }

        /// <inheritdoc />
        public override bool Equals( object? obj ) => obj is License license && this._licenseKey == license._licenseKey;

        /// <inheritdoc />
        public override int GetHashCode() => 668981160 + EqualityComparer<string>.Default.GetHashCode( this._licenseKey );

        /// <inheritdoc />
        public override string ToString() => this._licenseKey;
    }
}