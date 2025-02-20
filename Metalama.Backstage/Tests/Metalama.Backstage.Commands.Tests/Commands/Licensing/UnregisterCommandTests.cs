// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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