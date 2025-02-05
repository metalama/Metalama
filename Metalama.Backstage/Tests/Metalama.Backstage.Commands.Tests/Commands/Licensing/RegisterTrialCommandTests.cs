// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands.Licensing
{
    public class RegisterTrialCommandTests : LicensingCommandsTestsBase
    {
        private static readonly DateTime _evaluationStart = new( 2020, 1, 1, 0, 0, 0, DateTimeKind.Utc );

        private static readonly DateTime _invalidNextEvaluationStart = new( 2020, 1, 14, 0, 0, 0, DateTimeKind.Utc );

        private static readonly DateTime _validNextEvaluationStart = new( 2021, 1, 1, 0, 0, 0, DateTimeKind.Utc );

        public RegisterTrialCommandTests( ITestOutputHelper logger )
            : base( logger )
        {
            this.Time.Set( _evaluationStart );
        }

        [Fact]
        public async Task TrialRegistersInEmptyEnvironment()
        {
            await this.TestCommandAsync( "license try" );

            await this.TestCommandAsync( "license list", "Evaluation License" );
        }

        [Fact]
        public async Task TrialRegistrationFailsWithinNoEvaluationPeriod()
        {
            await this.TestCommandAsync( "license try" );

            this.Time.Set( _invalidNextEvaluationStart );

            await this.TestCommandAsync(
                "license try",
                $"You cannot start a new trial period until {_evaluationStart.AddDays( 120 + 45 ).ToString( CultureInfo.CurrentCulture )}.",
                1 );

            await this.TestCommandAsync( "license list", "Evaluation License" );
        }

        [Fact]
        public async Task TrialRegistersAfterNoEvaluationPeriod()
        {
            await this.TestCommandAsync( "license try" );

            this.Time.Set( _validNextEvaluationStart );
            await this.TestCommandAsync( "license try" );

            await this.TestCommandAsync( "license list", "Evaluation License" );
        }
    }
}