// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing.Licenses;
using System;

namespace Metalama.Backstage.Licensing.Consumption.Sources
{
    [PublicAPI]
    public abstract class LicenseSourceBase : ILicenseSource
    {
        private readonly IServiceProvider _services;

        public abstract string Description { get; }

        public abstract LicenseSourceKind Kind { get; }

        protected LicenseSourceBase( IServiceProvider services )
        {
            this._services = services;
        }

        protected abstract string? GetLicenseString();

        public ILicense? GetLicense( Action<LicensingMessage> reportMessage )
        {
            var licenseFactory = new LicenseFactory( this._services );

            var licenseString = this.GetLicenseString();

            if ( string.IsNullOrWhiteSpace( licenseString ) )
            {
                return null;
            }

            if ( !licenseFactory.TryCreate( licenseString, out var license, out var errorMessage ) )
            {
                reportMessage( new LicensingMessage( errorMessage ) { IsError = true } );

                return null;
            }

            return license;
        }

        public event Action? Changed;

        public abstract LicenseSourcePriority Priority { get; }

        protected virtual void OnChanged() => this.Changed?.Invoke();

        public override string ToString() => this.GetType().Name;
    }
}