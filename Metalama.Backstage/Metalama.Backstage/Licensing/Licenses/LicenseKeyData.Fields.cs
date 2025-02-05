// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Licenses.LicenseFields;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Backstage.Licensing.Licenses
{
    public partial record LicenseKeyData
    {
        private readonly ImmutableSortedDictionary<LicenseFieldIndex, LicenseField> _fields = ImmutableSortedDictionary<LicenseFieldIndex, LicenseField>.Empty;

        IReadOnlyDictionary<LicenseFieldIndex, LicenseField> ILicenseKeyData.Fields => this._fields;

        private object? GetFieldValue( LicenseFieldIndex index )
        {
            if ( this._fields.TryGetValue( index, out var licenseField ) )
            {
                return licenseField.Value;
            }
            else
            {
                return null;
            }
        }

        public bool HasValidSignature { get; init; }

        public string? LicenseString { get; internal init; }

        /// <summary>
        /// Gets the license version.
        /// </summary>
        public byte Version { get; private init; }

        public Guid? LicenseGuid { get; init; }

        /// <summary>
        /// Gets the type of license.
        /// </summary>
        public LicenseType LicenseType { get; init; }

        /// <summary>
        /// Gets the licensed product.
        /// </summary>
        public LicensedProduct Product { get; init; }

        /// <summary>
        /// Gets the identifier of the current license.
        /// </summary>
        public int LicenseId { get; init; }

        /// <summary>
        /// Gets the licensed namespace.
        /// </summary>
        public string? Namespace => (string?) this.GetFieldValue( LicenseFieldIndex.Namespace );

        /// <summary>
        /// Gets a value indicating whether the license usage can be audited.
        /// </summary>
        public bool? Auditable => (bool?) this.GetFieldValue( LicenseFieldIndex.Auditable );

        /// <summary>
        /// Gets the licensed public key token.
        /// </summary>
        public byte[]? PublicKeyToken => (byte[]?) this.GetFieldValue( LicenseFieldIndex.PublicKeyToken );

        /// <summary>
        /// Gets the signature.
        /// </summary>
        public byte[]? Signature => (byte[]?) this.GetFieldValue( LicenseFieldIndex.Signature );

        /// <summary>
        /// Gets the identifier of the signature.
        /// </summary>
        public byte? SignatureKeyId => (byte?) this.GetFieldValue( LicenseFieldIndex.SignatureKeyId );

        /// <summary>
        /// Gets the allowed number of users.
        /// </summary>
        public short? UserNumber => (short?) this.GetFieldValue( LicenseFieldIndex.UserNumber );

        /// <summary>
        /// Gets the date from which the license is valid.
        /// </summary>
        public DateTime? ValidFrom => (DateTime?) this.GetFieldValue( LicenseFieldIndex.ValidFrom );

        /// <summary>
        /// Gets the date to which the license is valid.
        /// </summary>
        public DateTime? ValidTo => (DateTime?) this.GetFieldValue( LicenseFieldIndex.ValidTo );

        public DateTime? SubscriptionEndDate => (DateTime?) this.GetFieldValue( LicenseFieldIndex.SubscriptionEndDate );

        /// <summary>
        /// Gets the full name of the licensee.
        /// </summary>
        public string? Licensee => (string?) this.GetFieldValue( LicenseFieldIndex.Licensee );

        /// <summary>
        /// Gets the hash of the licensee name.
        /// </summary>
        public int? LicenseeHash => (int?) this.GetFieldValue( LicenseFieldIndex.LicenseeHash );

        /// <summary>
        /// Gets the number of days in the grace period.
        /// </summary>
        public byte GraceDays => (byte?) this.GetFieldValue( LicenseFieldIndex.GraceDays ) ?? 30;

        /// <summary>
        /// Gets the number of percents of additional users allowed during the grace period.
        /// </summary>
        public byte? GracePercent => (byte?) this.GetFieldValue( LicenseFieldIndex.GracePercent );

        /// <summary>
        /// Gets the number of authorized devices per user.
        /// </summary>
        public byte? DevicesPerUser => (byte?) this.GetFieldValue( LicenseFieldIndex.DevicesPerUser );

        public bool? AllowInheritance => (bool?) this.GetFieldValue( LicenseFieldIndex.AllowInheritance );

        public bool? LicenseServerEligible => (bool?) this.GetFieldValue( LicenseFieldIndex.LicenseServerEligible );

        public Version? MinPostSharpVersion
        {
            get
            {
                var minPostSharpVersionString = (string?) this.GetFieldValue( LicenseFieldIndex.MinPostSharpVersion );

                return minPostSharpVersionString == null ? null : System.Version.Parse( minPostSharpVersionString );
            }
        }
        
        internal LicenseKeyDataBuilder ToBuilder()
            => new( this._fields ) { Product = this.Product, LicenseId = this.LicenseId, LicenseType = this.LicenseType, LicenseGuid = this.LicenseGuid };
    }
}