// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing.Licenses.LicenseFields;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Metalama.Backstage.Licensing.Licenses
{
    /// <summary>
    /// Provides serialization, cryptography and validation for license keys.
    /// </summary>
    [PublicAPI( "Used by the license audit loader." )]
    public partial record LicenseKeyData : ILicenseKeyData
    {
        public string LicenseUniqueId
            => this.LicenseGuid.HasValue
                ? this.LicenseGuid.Value.ToString()
                : this.LicenseId.ToString( CultureInfo.InvariantCulture );

        public bool RequiresSignature() => LicenseKeyDataSerializer.RequiresSignature( this );

        // TODO in Metalama
        public bool RequiresWatermark => this.LicenseType is LicenseType.Evaluation or LicenseType.Academic;

        /// <summary>
        /// Gets a value indicating whether the license is a redistribution license.
        /// </summary>
        [Obsolete]
        public bool IsRedistribution => this.LicenseType is LicenseType.OpenSourceRedistribution or LicenseType.CommercialRedistribution;

        /// <summary>
        /// Gets a value indicating whether the license is limited by a namespace.
        /// </summary>
        [Obsolete]
        public bool IsLimitedByNamespace => !string.IsNullOrEmpty( this.Namespace );

        /// <summary>
        /// The first version of Metalama that verifies an Elliptic Curve DSA signature, which is the licensing
        /// authority added by issue #1864. An earlier version has no authority of the identifiers of the keys of that
        /// algorithm, so it reports that the signature of the license key is invalid.
        /// </summary>
        private static readonly Version _firstVersionSupportingECDsaSignature = new( 2027, 0 );

        /// <summary>
        /// Gets the minimal version of Metalama that can consume the current license key, or <c>null</c> if every
        /// version can consume it. The value is detected from the properties of the license key, so that it does not
        /// depend on the license generator. Registration stores the license key in the group of that version, so that
        /// the versions which cannot consume the license key never read it. See issue #1922.
        /// </summary>
        /// <remarks>
        /// The licensing authority that signs the license key is the only property that decides the minimal version
        /// today. A must-understand license field that an earlier version does not declare is the next property to
        /// decide it.
        /// </remarks>
        public Version? MinMetalamaVersion
            => this.SignatureKeyId switch
            {
                // The key 2 of ProductionLicensingAuthorityProvider and the key 254 of TestLicensingAuthorityProvider
                // are the Elliptic Curve DSA keys. The other identifiers are those of the finite field DSA keys,
                // which every version verifies.
                2 or TestLicensingAuthorityProvider.ECDsaTestKeyId => _firstVersionSupportingECDsaSignature,
                _ => null
            };

        internal LicenseKeyData() : this( LicenseKeyDataSerializer.CurrentVersion, ImmutableSortedDictionary<LicenseFieldIndex, LicenseField>.Empty ) { }

        internal LicenseKeyData( byte version, ImmutableSortedDictionary<LicenseFieldIndex, LicenseField> fields )
        {
            this.Version = version;
            this._fields = fields;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder stringBuilder = new();

            stringBuilder.AppendFormat(
                CultureInfo.InvariantCulture,
                "Version={0}, LicenseId={1}, LicenseType={2}, Product={3}",
                this.Version,
                this.LicenseId,
                this.LicenseType,
                this.Product );

            foreach ( var licenseField in this._fields )
            {
                stringBuilder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    ", {0}={{{1}}}",
                    licenseField.Key,
                    licenseField.Value );
            }

            return stringBuilder.ToString();
        }

        private static readonly ConcurrentDictionary<string, LicenseKeyData> _cache = new();

        public static bool TryDeserialize(
            string licenseKey,
            [NotNullWhen( true )] out LicenseKeyData? data,
            [NotNullWhen( false )] out string? errorMessage )
        {
            if ( _cache.TryGetValue( licenseKey, out data ) )
            {
                errorMessage = null;

                return true;
            }
            else
            {
                if ( !LicenseKeyDataBuilder.TryDeserialize( licenseKey, out var builder, out errorMessage ) )
                {
                    return false;
                }

                data = builder.Build();
                _cache.TryAdd( licenseKey, data );

                return true;
            }
        }
    }
}