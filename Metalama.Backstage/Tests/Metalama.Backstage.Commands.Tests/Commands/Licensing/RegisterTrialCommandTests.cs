// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands.Licensing
{
    public sealed class RegisterTrialCommandTests : LicensingCommandsTestsBase
    {
        private static readonly DateTime _evaluationStart = new( 2020, 1, 1, 0, 0, 0, DateTimeKind.Utc );

        private static readonly DateTime _invalidNextEvaluationStart = new( 2020, 3, 1, 0, 0, 0, DateTimeKind.Utc );

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

            this.Time.Set( _invalidNextEvaluationStart.AddMinutes( 1 ) );

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