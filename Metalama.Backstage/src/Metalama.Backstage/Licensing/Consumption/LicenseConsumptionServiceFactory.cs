// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Licensing.Licenses;
using System;
using System.Collections.Generic;

namespace Metalama.Backstage.Licensing.Consumption;

internal static class LicenseConsumptionServiceFactory
{
    public static ILicenseConsumptionService Create(
        IServiceProvider serviceProvider,
        LicensingInitializationOptions options )
    {
        var licenseSources = new List<ILicenseSource>();

        if ( (options.IgnoredLicenseSources & LicenseSourceKind.Unattended) == 0 )
        {
            licenseSources.Add( new UnattendedLicenseSource( serviceProvider ) );
        }

        if ( (options.IgnoredLicenseSources & LicenseSourceKind.UserProfile) == 0 )
        {
            licenseSources.Add( new UserProfileLicenseSource( serviceProvider ) );
        }

        if ( options.BuildTestLicenseAction != null )
        {
            var licenseBuilder = new LicenseKeyDataBuilder();
            options.BuildTestLicenseAction( licenseBuilder );
            var licenseKey = licenseBuilder.SignAndSerialize( LicensingAuthority.GetTestAuthority() );
            licenseSources.Add( new ExplicitLicenseSource( licenseKey, LicenseSourceKind.Test, serviceProvider ) );
        }

        return new LicenseConsumptionService( serviceProvider, licenseSources );
    }
}