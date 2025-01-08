// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands.Licensing
{
    public class RegisterCommunityCommandTests : LicensingCommandsTestsBase
    {
        public RegisterCommunityCommandTests( ITestOutputHelper logger )
            : base( logger ) { }

        [Fact]
        public async Task FreeRegistersInEmptyEnvironment()
        {
            await this.TestCommandAsync( "license community" );

            await this.TestCommandAsync( "license list", "Metalama Community" );
        }

        [Fact]
        public async Task RepetitiveFreeRegistrationKeepsOneFreeLicenseRegistered()
        {
            await this.TestCommandAsync( "license community" );
            await this.TestCommandAsync( "license community" );

            await this.TestCommandAsync( "license list", "Metalama Community" );
        }
    }
}