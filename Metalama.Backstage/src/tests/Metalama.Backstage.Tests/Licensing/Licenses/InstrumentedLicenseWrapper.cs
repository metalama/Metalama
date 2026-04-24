// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Tests.Licensing.Licenses
{
    internal sealed class InstrumentedLicenseWrapper : ILicense, IUsable
    {
        private readonly ILicense _license;

        public int NumberOfAuditReports { get; private set; }

        public InstrumentedLicenseWrapper( ILicense license )
        {
            this._license = license;
        }

        // MaybeNullWhenAttribute cannot be used here since the Metalama.Backstage assembly shares internals with this assembly.
        // That causes CS0433 error. (Same type defined in two referenced assemblies.)
        public bool CanBeRegistered( [MaybeNullWhen( true )] out string errorMessage ) => throw new NotImplementedException();

        public bool TryGetConsumptionProperties( /* [MaybeNullWhenAttribute( false )] */
            LicenseConsumptionOptions options,
            out LicenseConsumptionProperties licenseProperties,
            out string errorMessage )
            => this._license.TryGetConsumptionProperties( options, out licenseProperties!, out errorMessage! );

        // MaybeNullWhenAttribute cannot be used here since the Metalama.Backstage assembly shares internals with this assembly.
        // That causes CS0433 error. (Same type defined in two referenced assemblies.)
        public bool TryGetRegistrationProperties( /* [MaybeNullWhenAttribute( false )] */
            out LicenseRegistrationProperties licenseProperties,
            out string errorMessage )
            => this._license.TryGetRegistrationProperties( out licenseProperties!, out errorMessage! );

        public void ReportUse()
        {
            this.NumberOfAuditReports++;
            this._license.ReportUse();
        }

        public void ResetNumberOfAuditReports()
        {
            this.NumberOfAuditReports = 0;
        }

        public override bool Equals( object? obj )
        {
            return obj is InstrumentedLicenseWrapper license &&
                   EqualityComparer<ILicense>.Default.Equals( this._license, license._license );
        }

        public override int GetHashCode()
        {
            return HashCode.Combine( this._license );
        }
    }
}