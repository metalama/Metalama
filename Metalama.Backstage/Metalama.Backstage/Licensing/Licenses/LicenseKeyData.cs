// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing.Licenses.LicenseFields;
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
    [PublicAPI]
    public partial record LicenseKeyData : ILicenseKeyData
    {
        public bool RequiresSignature => this.RequiresSignature();

        public string LicenseUniqueId
            => this.LicenseGuid.HasValue
                ? this.LicenseGuid.Value.ToString()
                : this.LicenseId.ToString( CultureInfo.InvariantCulture );

        // TODO in Metalama
        public bool RequiresWatermark => this.LicenseType == LicenseType.Evaluation || this.LicenseType == LicenseType.Academic;

        /// <summary>
        /// Gets a value indicating whether the license is a redistribution license.
        /// </summary>
        public bool IsRedistribution => this.LicenseType == LicenseType.OpenSourceRedistribution || this.LicenseType == LicenseType.CommercialRedistribution;

        /// <summary>
        /// Gets a value indicating whether the license is limited by a namespace.
        /// </summary>
        public bool IsLimitedByNamespace => !string.IsNullOrEmpty( this.Namespace );

        internal LicenseKeyData() : this( ImmutableSortedDictionary<LicenseFieldIndex, LicenseField>.Empty ) { }

        internal LicenseKeyData( ImmutableSortedDictionary<LicenseFieldIndex, LicenseField> fields )
        {
            this.Version = 2;
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
                    ", {0}={1}",
                    licenseField.Key,
                    licenseField.Value );
            }

            return stringBuilder.ToString();
        }

        private static readonly ConcurrentDictionary<string, LicenseKeyData> _cache = new();

        public static bool TryDeserialize(
            string licenseKey,
            [MaybeNullWhen( false )] out LicenseKeyData data,
            [MaybeNullWhen( true )] out string errorMessage )
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

        private static bool ComparePublicKeyToken( byte[]? key1, byte[]? key2 )
        {
            if ( key1 == null )
            {
                return key2 == null;
            }

            if ( key2 == null )
            {
                return false;
            }

            for ( var i = 0; i < key1.Length; i++ )
            {
                if ( key1[i] != key2[i] )
                {
                    return false;
                }
            }

            return true;
        }
    }
}