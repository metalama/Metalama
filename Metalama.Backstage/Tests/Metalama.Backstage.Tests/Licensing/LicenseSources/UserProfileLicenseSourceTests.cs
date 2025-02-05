// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Consumption.Sources;
using System.IO.Abstractions.TestingHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.LicenseSources
{
    public class UserProfileLicenseSourceTests : LicensingTestsBase
    {
        private const string _licenseFilePath = "licensing.json";

        public UserProfileLicenseSourceTests( ITestOutputHelper logger )
            : base( logger ) { }

        [Fact]
        public void NonexistentFileIsReported()
        {
            UserProfileLicenseSource source = new( this.ServiceProvider );

            Assert.Null( source.GetLicense( _ => { } ) );
        }

        [Fact]
        public void EmptyFilePasses()
        {
            this.FileSystem.Mock.AddFile( _licenseFilePath, new MockFileData( "" ) );

            UserProfileLicenseSource source = new( this.ServiceProvider );

            Assert.Null( source.GetLicense( _ => { } ) );
        }
    }
}