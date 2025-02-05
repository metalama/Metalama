// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
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
    public class License : ILicense
    {
        private readonly string _licenseKey;

        private readonly IServiceProvider _services;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger _logger;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="License"/> class.
        /// </summary>
        /// <param name="licenseKey">The license key.</param>
        /// <param name="services">Services.</param>
        internal License( string licenseKey, IServiceProvider services )
        {
            this._licenseKey = CleanLicenseKey( licenseKey );
            this._services = services;
            this._dateTimeProvider = services.GetRequiredBackstageService<IDateTimeProvider>();
            this._logger = services.GetLoggerFactory().Licensing();
        }

        /// <inheritdoc />
        public bool TryGetLicenseConsumptionData(
            [MaybeNullWhen( false )] out LicenseConsumptionData licenseConsumptionData,
            [MaybeNullWhen( true )] out string errorMessage )
        {
            if ( !this.TryGetValidatedLicenseKeyData( out var licenseKeyData, out errorMessage ) )
            {
                licenseConsumptionData = null;

                return false;
            }

            licenseConsumptionData = licenseKeyData.ToConsumptionData();

            return true;
        }

        /// <inheritdoc />
        public bool TryGetProperties(
            [MaybeNullWhen( false )] out LicenseProperties licenseProperties,
            [MaybeNullWhen( true )] out string errorMessage )
        {
            if ( !this.TryGetLicenseKeyDataWithVerifiedSignature( out var licenseKeyData, out errorMessage ) )
            {
                licenseProperties = null;

                return false;
            }

            licenseProperties = licenseKeyData.ToLicenseProperties();

            return true;
        }

        private bool TryGetLicenseKeyData( [MaybeNullWhen( false )] out LicenseKeyData data, [MaybeNullWhen( true )] out string errorMessage )
        {
            this._logger.Trace?.Log( $"Deserializing license '{this._licenseKey}'." );

            if ( LicenseKeyData.TryDeserialize( this._licenseKey, out data, out _ ) )
            {
                this._logger.Trace?.Log( $"Deserialized license: {data}" );

                errorMessage = null;

                return true;
            }
            else
            {
                errorMessage = $"Cannot parse the license key '{this._licenseKey}'.";
  
                this._logger.Error?.Log( errorMessage );

                return false;
            }
        }

        private bool TryGetValidatedLicenseKeyData(
            [MaybeNullWhen( false )] out LicenseKeyData data,
            [MaybeNullWhen( true )] out string validationErrorMessage )
        {
            if ( !this.TryGetLicenseKeyData( out data, out validationErrorMessage ) )
            {
                return false;
            }

            var applicationInfoService = this._services.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;

            if ( !data.Validate(
                    null,
                    this._dateTimeProvider,
                    applicationInfoService,
                    out validationErrorMessage ) )
            {
                this._logger.Warning?.Log( $"The license key {data.LicenseUniqueId} is invalid: {validationErrorMessage}" );
                data = null;

                return false;
            }

            return true;
        }

        private bool TryGetLicenseKeyDataWithVerifiedSignature(
            [MaybeNullWhen( false )] out LicenseKeyData data,
            [MaybeNullWhen( true )] out string errorMessage )
        {
            if ( !this.TryGetLicenseKeyData( out data, out errorMessage ) )
            {
                return false;
            }

            if ( data is { RequiresSignature: true, HasValidSignature: false } )
            {
                errorMessage = $"The license key {data.LicenseUniqueId} has an invalid signature.";
                this._logger.Warning?.Log( errorMessage );
                data = null;

                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals( object? obj ) => obj is License license && this._licenseKey == license._licenseKey;

        /// <inheritdoc />
        public override int GetHashCode() => 668981160 + EqualityComparer<string>.Default.GetHashCode( this._licenseKey );

        /// <inheritdoc />
        public override string ToString() => this._licenseKey;
    }
}