// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Consumption.Sources;
using System.IO.Abstractions.TestingHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.LicenseSources
{
    public sealed class UserProfileLicenseSourceTests : LicensingTestsBase
    {
        private const string _licenseFilePath = "licensing.json";

        public UserProfileLicenseSourceTests( ITestOutputHelper logger )
            : base( logger ) { }

        [Fact]
        public void NonexistentFileIsReported()
        {
            UserProfileLicenseSource source = new( this.ServiceProvider );

            Assert.Empty( source.GetLicenses( _ => { } ) );
        }

        [Fact]
        public void EmptyFilePasses()
        {
            this.FileSystem.Mock.AddFile( _licenseFilePath, new MockFileData( "" ) );

            UserProfileLicenseSource source = new( this.ServiceProvider );

            Assert.Empty( source.GetLicenses( _ => { } ) );
        }
    }
}