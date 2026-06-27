// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Testing;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands.Telemetry
{
    public sealed class TelemetryStatusCommandTests : CommandsTestsBase
    {
        private static readonly TestLicenseKeyProvider _licenseKeyProvider = new();

        private const string _licenseAuditWarning = "license audit data is uploaded";

        public TelemetryStatusCommandTests( ITestOutputHelper logger )
            : base( logger )
        {
            this.UserDeviceDetection.IsInteractiveDevice = true;
        }

        [Fact]
        public async Task NoLicense_NoLicenseAuditWarning()
        {
            await this.TestCommandAsync( "telemetry status", unexpectedOutput: _licenseAuditWarning );
        }

        [Fact]
        public async Task AuditableLicense_ShowsLicenseAuditWarning()
        {
            await this.TestCommandAsync( $"license register {_licenseKeyProvider.MetalamaProfessionalBusiness}" );
            await this.TestCommandAsync( "telemetry status", expectedOutput: _licenseAuditWarning );
        }

        [Fact]
        public async Task NonAuditableLicense_NoLicenseAuditWarning()
        {
            await this.TestCommandAsync( $"license register {_licenseKeyProvider.MetalamaProfessionalBusinessNotAuditable}" );
            await this.TestCommandAsync( "telemetry status", unexpectedOutput: _licenseAuditWarning );
        }
    }
}
