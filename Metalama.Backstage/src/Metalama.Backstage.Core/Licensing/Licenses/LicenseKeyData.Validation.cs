// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Licenses.LicenseFields;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;

namespace Metalama.Backstage.Licensing.Licenses
{
    public partial record LicenseKeyData
    {
        public bool ValidateFields( [NotNullWhen( false )] out string? errorMessage )
        {
            if ( !Enum.IsDefined( typeof(LicenseType), this.LicenseType ) )
            {
                errorMessage = "the license key license type is unknown";

                return false;
            }

            if ( !Enum.IsDefined( typeof(LicenseProduct), this.Product ) )
            {
                errorMessage = "the license key licensed product is unknown";

                return false;
            }

            if ( this._fields.Keys.Any(
                    i =>
                        i.IsMustUnderstand()
                        && !Enum.IsDefined( typeof(LicenseFieldIndex), i ) ) )
            {
                errorMessage = "the license key contains unknown must-understand fields";

                return false;
            }

            errorMessage = null;

            return true;
        }

        /// <summary>
        /// Verifies the signature of the current license key, if it requires one.
        /// </summary>
        /// <param name="licensingAuthorityProvider">The provider of the authority of the key that the signature was created with.</param>
        /// <param name="errorMessage">At output, the reason why the signature was not verified, or <c>null</c> if it was.</param>
        /// <returns><c>true</c> if the current license key requires no signature or if its signature is valid, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// The signature algorithm of a license key is chosen when the license key is issued, so a license key can
        /// require an algorithm that the current platform does not implement. Finite field DSA, which signs every
        /// license key issued until 2026, is unavailable on macOS since .NET 11. That case is reported through
        /// <paramref name="errorMessage"/> and is not raised as an exception, because it is a property of the license
        /// key and of the platform, not a defect of the product.
        /// </remarks>
        public bool TryVerifySignature(
            ILicensingAuthorityProvider licensingAuthorityProvider,
            [MaybeNullWhen( true )] out string errorMessage )
        {
            const string invalidSignature = "the license key has an invalid signature";

            try
            {
                if ( !this.RequiresSignature() )
                {
                    errorMessage = null;

                    return true;
                }

                if ( this.Signature == null || this.SignatureKeyId == null )
                {
                    errorMessage = invalidSignature;

                    return false;
                }

                var buffer = this.GetSignedBuffer();

                if ( !licensingAuthorityProvider.GetAuthority( this.SignatureKeyId.Value ).VerifySignature( buffer, this.Signature ) )
                {
                    errorMessage = invalidSignature;

                    return false;
                }

                errorMessage = null;

                return true;
            }
            catch ( CryptographicException )
            {
                errorMessage = invalidSignature;

                return false;
            }
            catch ( PlatformNotSupportedException )
            {
                errorMessage = "the license key is signed with a cryptographic algorithm that this platform does not support";

                return false;
            }
        }
    }
}