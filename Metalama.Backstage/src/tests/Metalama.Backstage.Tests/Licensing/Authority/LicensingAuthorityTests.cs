// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Licenses;
using System.Security.Cryptography;
using Xunit;

namespace Metalama.Backstage.Tests.Licensing.Authority;

public sealed class LicensingAuthorityTests
{
    [Fact]
    public void TestCreateSigningAuthorityFromXml()
    {
        var key = DSA.Create();
        var privateKey = key.ToXmlString( true );
        var publicKey = key.ToXmlString( false );
        var signingLicensingAuthority = new LicensingAuthority( (100, privateKey) );
        var verifyingLicensingAuthority = new LicensingAuthority( (100, publicKey) );

        byte[] message = [1, 2, 3];
        signingLicensingAuthority.Sign( message, out var signature );

        Assert.True( verifyingLicensingAuthority.VerifySignature( message, 100, signature ) );
    }
}