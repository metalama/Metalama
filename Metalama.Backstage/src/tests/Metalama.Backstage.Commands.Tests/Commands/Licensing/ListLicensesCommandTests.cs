// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands.Licensing
{
    public sealed class ListLicensesCommandTests : LicensingCommandsTestsBase
    {
        public ListLicensesCommandTests( ITestOutputHelper logger )
            : base( logger ) { }

        [Fact]
        public async Task AuditableLicense_LicenseAuditRowShowsYes()
        {
            await this.TestCommandAsync( $"license register {LicenseKeyProvider.MetalamaProfessionalBusiness}" );
            await this.TestCommandAsync( "license list", expectedOutput: "Yes" );
        }

        [Fact]
        public async Task NonAuditableLicense_LicenseAuditRowShowsNo()
        {
            await this.TestCommandAsync( $"license register {LicenseKeyProvider.MetalamaProfessionalBusinessNotAuditable}" );
            await this.TestCommandAsync( "license list", expectedOutput: "No" );
        }

        /// <summary>
        /// Tests that a group of license keys that the current version does not support is reported as requiring a
        /// later version of Metalama, instead of being hidden. The license key of the group does not parse, so the
        /// command reports the version of the group without deserializing what it contains. See issue #1922.
        /// </summary>
        [Fact]
        public async Task UnsupportedGroup_IsReportedAsRequiringLaterVersion()
        {
            this.AddUnsupportedLicenseGroup();

            await this.TestCommandAsync( "license list", expectedOutput: $"requires Metalama {UnsupportedVersion} or later" );
        }

        /// <summary>
        /// Tests that a group of license keys that the current version does not support neither hides the license
        /// keys that the current version does consume, nor makes the command fail. See issue #1922.
        /// </summary>
        [Fact]
        public async Task UnsupportedGroup_DoesNotHideSupportedLicense()
        {
            await this.TestCommandAsync( $"license register {LicenseKeyProvider.MetalamaProfessionalBusiness}" );

            this.AddUnsupportedLicenseGroup();

            await this.TestCommandAsync( "license list", expectedOutput: "Metalama Professional" );
            await this.TestCommandAsync( "license list", expectedOutput: $"requires Metalama {UnsupportedVersion} or later" );
        }
    }
}
