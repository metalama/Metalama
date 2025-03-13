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

        public bool VerifySignature( LicensingAuthority licensingAuthority )
        {
            try
            {
                if ( !this.RequiresSignature() )
                {
                    return true;
                }

                if ( this.Signature == null || this.SignatureKeyId == null )
                {
                    return false;
                }

                var buffer = this.GetSignedBuffer();

                return licensingAuthority.VerifySignature( buffer, this.SignatureKeyId.Value, this.Signature );
            }
            catch ( CryptographicException )
            {
                return false;
            }
        }
    }
}