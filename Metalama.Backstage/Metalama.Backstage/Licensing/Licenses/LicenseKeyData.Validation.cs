// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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