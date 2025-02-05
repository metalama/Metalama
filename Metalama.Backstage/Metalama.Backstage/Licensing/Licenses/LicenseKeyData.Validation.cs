// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Licenses.LicenseFields;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Backstage.Licensing.Licenses
{
    public partial record LicenseKeyData
    {
        internal bool Validate(
            byte[]? publicKeyToken,
            IDateTimeProvider dateTimeProvider,
            IApplicationInfo applicationInfo,
            [MaybeNullWhen( true )] out string errorDescription )
        {
#pragma warning disable CS0618
            if ( this.LicenseType == LicenseType.Anonymous )
            {
                // Anonymous licenses are always valid but confer no right.
                errorDescription = null;

                return true;
            }
#pragma warning restore CS0618

            if ( this.LicenseId is not 0 and < 20 )
            {
                errorDescription = "has been revoked";

                return false;
            }

            if ( this.RequiresSignature && !this.HasValidSignature )
            {
                errorDescription = "has an invalid signature";

                return false;
            }

            if ( this.ValidFrom.HasValue && this.ValidFrom > dateTimeProvider.UtcNow )
            {
                errorDescription = "is not yet valid";

                return false;
            }

            if ( this.ValidTo.HasValue && this.ValidTo < dateTimeProvider.UtcNow )
            {
                errorDescription = "has expired";

                return false;
            }

            if ( this.PublicKeyToken != null )
            {
                if ( publicKeyToken == null )
                {
                    errorDescription = "cannot be validated because the validating assembly is missing a public key token";

                    return false;
                }

                if ( !ComparePublicKeyToken( publicKeyToken, this.PublicKeyToken ) )
                {
                    errorDescription = "is invalid because the public key token of the assembly does not match the license";

                    return false;
                }
            }

            if ( !Enum.IsDefined( typeof(LicenseType), this.LicenseType ) )
            {
                errorDescription = "license type is unknown";

                return false;
            }

            if ( !Enum.IsDefined( typeof(LicensedProduct), this.Product ) )
            {
                errorDescription = "licensed product is unknown";

                return false;
            }

            if ( this._fields.Keys.Any(
                    i =>
                        i.IsMustUnderstand()
                        && !Enum.IsDefined( typeof(LicenseFieldIndex), i ) ) )
            {
                errorDescription = "contains unknown must-understand fields";

                return false;
            }

            if ( this.SubscriptionEndDate.HasValue )
            {
                if ( !applicationInfo.BuildDate.HasValue )
                {
                    throw new InvalidOperationException( $"Application '{applicationInfo.Name}' is missing build date information." );
                }

                var latestComponentMadeByPostSharp = applicationInfo.GetLatestComponentMadeByPostSharp();

                if ( this.SubscriptionEndDate < latestComponentMadeByPostSharp.BuildDate )
                {
                    errorDescription =
                        $"does not allow to use the licensed product '{latestComponentMadeByPostSharp.Name}' version {latestComponentMadeByPostSharp.PackageVersion} released on {latestComponentMadeByPostSharp.BuildDate:d} - only versions released before {this.SubscriptionEndDate:d} are allowed to use by this license";

                    return false;
                }
            }

            if ( this.IsRedistribution && !this.IsLimitedByNamespace )
            {
                errorDescription = "is a redistribution license, but it is not limited by a namespace";

                return false;
            }

            errorDescription = null;

            return true;
        }
    }
}