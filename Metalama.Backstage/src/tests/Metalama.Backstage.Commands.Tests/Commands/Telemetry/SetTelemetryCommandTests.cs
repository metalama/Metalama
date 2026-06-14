// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Telemetry;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands.Telemetry
{
    public sealed class SetTelemetryCommandTests : CommandsTestsBase
    {
        public SetTelemetryCommandTests( ITestOutputHelper logger )
            : base( logger ) { }

        private ReportingAction GetAction( TelemetryScenario scenario )
            => this.ConfigurationManager!.Get<TelemetryConfiguration>().GetReportingAction( scenario );

        private void SetBaseline( bool enabled )
        {
            // Set a known baseline so the assertions don't depend on the first-run defaults.
            this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Usage, enabled );
            this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Exception, enabled );
            this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Performance, enabled );
        }

        [Fact]
        public async Task EnableExceptionAffectsOnlyException()
        {
            this.SetBaseline( false );

            await this.TestCommandAsync( "telemetry enable exception", "Exception telemetry has been enabled." );

            Assert.Equal( ReportingAction.Yes, this.GetAction( TelemetryScenario.Exception ) );
            Assert.Equal( ReportingAction.No, this.GetAction( TelemetryScenario.Usage ) );
            Assert.Equal( ReportingAction.No, this.GetAction( TelemetryScenario.Performance ) );
        }

        [Fact]
        public async Task EnablePerformanceAffectsOnlyPerformance()
        {
            this.SetBaseline( false );

            await this.TestCommandAsync( "telemetry enable performance", "Performance telemetry has been enabled." );

            Assert.Equal( ReportingAction.Yes, this.GetAction( TelemetryScenario.Performance ) );
            Assert.Equal( ReportingAction.No, this.GetAction( TelemetryScenario.Usage ) );
            Assert.Equal( ReportingAction.No, this.GetAction( TelemetryScenario.Exception ) );
        }

        [Fact]
        public async Task DisableUsageAffectsOnlyUsage()
        {
            this.SetBaseline( true );

            await this.TestCommandAsync( "telemetry disable usage", "Usage telemetry has been disabled." );

            Assert.Equal( ReportingAction.No, this.GetAction( TelemetryScenario.Usage ) );
            Assert.Equal( ReportingAction.Yes, this.GetAction( TelemetryScenario.Exception ) );
            Assert.Equal( ReportingAction.Yes, this.GetAction( TelemetryScenario.Performance ) );
        }

        [Fact]
        public async Task EnableAllAffectsEveryScenario()
        {
            this.SetBaseline( false );

            await this.TestCommandAsync( "telemetry enable all", "Telemetry has been enabled." );

            Assert.Equal( ReportingAction.Yes, this.GetAction( TelemetryScenario.Usage ) );
            Assert.Equal( ReportingAction.Yes, this.GetAction( TelemetryScenario.Exception ) );
            Assert.Equal( ReportingAction.Yes, this.GetAction( TelemetryScenario.Performance ) );
        }

        [Fact]
        public async Task DisableAllAffectsEveryScenario()
        {
            this.SetBaseline( true );

            await this.TestCommandAsync( "telemetry disable all", "Telemetry has been disabled." );

            Assert.Equal( ReportingAction.No, this.GetAction( TelemetryScenario.Usage ) );
            Assert.Equal( ReportingAction.No, this.GetAction( TelemetryScenario.Exception ) );
            Assert.Equal( ReportingAction.No, this.GetAction( TelemetryScenario.Performance ) );
        }

        [Fact]
        public async Task EnableWithoutScenarioDefaultsToAll()
        {
            this.SetBaseline( false );

            await this.TestCommandAsync( "telemetry enable", "Telemetry has been enabled." );

            Assert.Equal( ReportingAction.Yes, this.GetAction( TelemetryScenario.Usage ) );
            Assert.Equal( ReportingAction.Yes, this.GetAction( TelemetryScenario.Exception ) );
            Assert.Equal( ReportingAction.Yes, this.GetAction( TelemetryScenario.Performance ) );
        }
    }
}
