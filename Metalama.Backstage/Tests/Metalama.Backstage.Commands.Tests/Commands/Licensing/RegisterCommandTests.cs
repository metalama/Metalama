// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands.Licensing
{
    public sealed class RegisterCommandTests : LicensingCommandsTestsBase
    {
        public RegisterCommandTests( ITestOutputHelper logger )
            : base( logger ) { }

        [Fact]
        public async Task CleanEnvironmentListsNoLicenses()
        {
            await this.TestCommandAsync( "license list", "No Metalama license" );
        }

        [Fact]
        public async Task OneLicenseKeyListedAfterOneRegistration()
        {
            await this.TestCommandAsync( $"license register {LicenseKeyProvider.MetalamaProfessionalBusiness}" );

            await this.TestCommandAsync( "license list", LicenseKeyProvider.MetalamaProfessionalBusiness );
        }

        [Fact]
        public async Task OneLicenseKeyListedAfterMultipleLicenseKeysRegistered()
        {
            await this.TestCommandAsync( $"license register {LicenseKeyProvider.MetalamaProfessionalBusiness}" );
            await this.TestCommandAsync( $"license register {LicenseKeyProvider.MetalamaProfessionalPersonal}" );

            await this.TestCommandAsync( "license list", LicenseKeyProvider.MetalamaProfessionalPersonal );
        }
    }
}