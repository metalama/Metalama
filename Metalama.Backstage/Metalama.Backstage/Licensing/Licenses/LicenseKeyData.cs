// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
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