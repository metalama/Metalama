// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands.Licensing
{
    public class RegisterFreeCommandTests : LicensingCommandsTestsBase
    {
        public RegisterFreeCommandTests( ITestOutputHelper logger )
            : base( logger ) { }

        [Fact]
        public async Task FreeRegistersInEmptyEnvironment()
        {
            await this.TestCommandAsync( "license free" );

            await this.TestCommandAsync( "license list", "Metalama Free" );
        }

        [Fact]
        public async Task RepetitiveFreeRegistrationKeepsOneFreeLicenseRegistered()
        {
            await this.TestCommandAsync( "license free" );
            await this.TestCommandAsync( "license free" );

            await this.TestCommandAsync( "license list", "Metalama Free" );
        }
    }
}