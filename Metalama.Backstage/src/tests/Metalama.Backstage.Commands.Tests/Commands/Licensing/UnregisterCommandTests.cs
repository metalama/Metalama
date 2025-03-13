// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands.Licensing
{
    public sealed class UnregisterCommandTests : LicensingCommandsTestsBase
    {
        public UnregisterCommandTests( ITestOutputHelper logger )
            : base( logger ) { }

        [Fact]
        public async Task RegisteredLicenseUnregisters()
        {
            await this.TestCommandAsync( $"license register {LicenseKeyProvider.MetalamaProfessionalBusiness}" );

            await this.TestCommandAsync( "license list", "Metalama Professional" );

            await this.TestCommandAsync( "license unregister", "have been unregistered." );

            await this.TestCommandAsync( "license list", "No Metalama license" );
        }
    }
}