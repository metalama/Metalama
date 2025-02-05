// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Consumption.Sources;
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

        if ( !options.IgnoreUnattendedProcessLicense )
        {
            licenseSources.Add( new UnattendedLicenseSource( serviceProvider ) );
        }

        if ( !options.IgnoreUserProfileLicenses )
        {
            licenseSources.Add( new UserProfileLicenseSource( serviceProvider ) );
        }

        return new LicenseConsumptionService( serviceProvider, licenseSources );
    }
}