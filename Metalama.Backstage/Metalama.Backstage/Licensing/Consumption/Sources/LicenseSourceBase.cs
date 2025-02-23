// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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