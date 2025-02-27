// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Licensing.Licenses;
using System;
using System.Collections.Generic;

namespace Metalama.Backstage.Tests.Licensing.LicenseSources
{
    internal sealed class TestLicenseSource : ILicenseSource, IUsable
    {
        private readonly ILicense? _license;

        public string Description => "test license source";

        public LicenseSourceKind Kind => LicenseSourceKind.Test;

        [UsedImplicitly]
        public string Id { get; }

        public int NumberOfUses { get; private set; }

        public TestLicenseSource( string id, ILicense? license )
        {
            this.Id = id;
            this._license = license;
        }

        public IEnumerable<ILicense> GetLicenses( Action<LicensingMessage> reportMessage )
        {
            this.NumberOfUses++;

            if ( this._license == null )
            {
                return [];
            }
            else
            {
                return [this._license];
            }
        }

        event Action? ILicenseSource.Changed { add { } remove { } }

        public LicenseSourcePriority Priority => LicenseSourcePriority.UserProfile;
    }
}