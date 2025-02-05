// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Testing;
using System;

namespace Metalama.Backstage.Tests.Licensing;

internal static class TestLicensingConfigurationHelpers
{
    public static string? ReadStoredLicenseString( IServiceProvider serviceProvider )
        => serviceProvider.GetRequiredBackstageService<IConfigurationManager>().Get<LicensingConfiguration>().License;

    public static void SetStoredLicenseString( IServiceProvider serviceProvider, string licenseString )
    {
        var configuration = new LicensingConfiguration { License = licenseString };

        ((TestFileSystem) serviceProvider.GetRequiredBackstageService<IFileSystem>()).Mock.AddFile(
            serviceProvider.GetRequiredBackstageService<IConfigurationManager>().GetFilePath<LicensingConfiguration>(),
            new MockFileDataEx( configuration.ToJson() ) );
    }
}