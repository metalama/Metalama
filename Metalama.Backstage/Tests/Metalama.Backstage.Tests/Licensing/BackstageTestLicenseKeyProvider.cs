// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Testing;

namespace Metalama.Backstage.Tests.Licensing;

public static class BackstageTestLicenseKeyProvider
{
    static BackstageTestLicenseKeyProvider()
    {
        LicenseKeyProvider = new TestLicenseKeyProvider( typeof(BackstageTestLicenseKeyProvider).Assembly );
    }
    
    public static TestLicenseKeyProvider LicenseKeyProvider { get; }
}