// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;
using System;
using System.Collections.Generic;

namespace Metalama.Backstage.Licensing.Consumption.Sources
{
    internal abstract class LicenseSourceBase : ILicenseSource
    {
        private readonly IServiceProvider _services;

        public abstract string Description { get; }

        public abstract LicenseSourceKind Kind { get; }

        protected LicenseSourceBase( IServiceProvider services )
        {
            this._services = services;
        }

        protected abstract IEnumerable<LicenseRegistrationProperties> GetRegisteredLicenses( Action<LicensingMessage> reportMessage );

        public IEnumerable<ILicense> GetLicenses( Action<LicensingMessage> reportMessage )
        {
            foreach ( var licenseProperties in this.GetRegisteredLicenses( reportMessage ) )
            {
                var licenseFactory = new LicenseFactory( this._services );

                if ( licenseFactory.TryCreate( licenseProperties.LicenseString, out var license, out var errorMessage ) )
                {
                    yield return license;
                }
                else
                {
                    reportMessage( new LicensingMessage( errorMessage ) );
                }
            }
        }

        public event Action? Changed;

        public abstract LicenseSourcePriority Priority { get; }

        protected virtual void OnChanged() => this.Changed?.Invoke();

        public override string ToString() => this.GetType().Name;
    }
}