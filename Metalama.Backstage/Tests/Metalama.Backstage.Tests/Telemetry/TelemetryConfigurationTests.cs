// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Telemetry;

public sealed class TelemetryConfigurationTests : TestsBase
{
    public TelemetryConfigurationTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } ) { }

    [Theory]
    [InlineData( null, true )]
    [InlineData( true, true )]
    [InlineData( false, false )]
    public void SetStatus( bool? input, bool output )
    {
        this.TelemetryConfigurationService.SetStatus( input );
        Assert.Equal( output, this.TelemetryConfigurationService.IsEnabled );
    }

    [Fact]
    public void EnabledByDefault()
    {
        Assert.True( this.TelemetryConfigurationService.IsEnabled );
    }

    [Theory]
    [InlineData( null, true )]
    [InlineData( "", true )]
    [InlineData( "true", false )]
    [InlineData( "anything", false )]
    [InlineData( "false", true )]
    [InlineData( "FALSE", true )]
    [InlineData( "False", true )]
    [InlineData( "0", true )]
    public void DisabledWithEnvironmentVariable( string? value, bool isEnabled )
    {
        if ( value != null )
        {
            this.EnvironmentVariableProvider.Environment[Backstage.Telemetry.TelemetryConfigurationService.OptOutEnvironmentVariable] = value;
        }

        this.TelemetryConfigurationService.SetStatus( true );
        Assert.Equal( isEnabled, this.TelemetryConfigurationService.IsEnabled );
    }
}