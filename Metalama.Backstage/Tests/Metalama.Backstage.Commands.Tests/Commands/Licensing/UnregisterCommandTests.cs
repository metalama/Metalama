// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands.Licensing
{
    public class UnregisterCommandTests : LicensingCommandsTestsBase
    {
        public UnregisterCommandTests( ITestOutputHelper logger )
            : base( logger ) { }

        [Fact]
        public async Task LicenseKeyFailsToUnregisterInCleanEnvironment()
        {
            await this.TestCommandAsync(
                $"license unregister",
                $"A license is not registered." + Environment.NewLine,
                1 );
        }

        [Fact]
        public async Task RegisteredLicenseUnregisters()
        {
            await this.TestCommandAsync( $"license register {LicenseKeyProvider.MetalamaStarterBusiness}" );

            await this.TestCommandAsync( "license list", "Metalama Starter" );

            await this.TestCommandAsync( "license unregister", "has been unregistered." );

            await this.TestCommandAsync( "license list", "No Metalama license" );
        }
    }
}