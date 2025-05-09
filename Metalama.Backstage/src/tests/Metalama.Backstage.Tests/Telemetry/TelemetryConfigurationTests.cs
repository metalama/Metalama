// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Telemetry;

public sealed class TelemetryConfigurationTests : TestsBase
{
    public TelemetryConfigurationTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } ) { }

    [Theory]
    [InlineData( true, true )]
    [InlineData( false, false )]
    public void SetStatus( bool input, bool output )
    {
        this.TelemetryConfigurationService.SetStatus( input );
        Assert.Equal( output, this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void EnabledByDefault()
    {
        Assert.True( this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Usage ) );
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
        Assert.Equal( isEnabled, this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void SaltRotation()
    {
        this.Time.Set( new DateTime( 2025, 4, 10, 0, 0, 0, DateTimeKind.Utc ) );
        this.TelemetryConfigurationService.Initialize();
        var initialSalt = this.TelemetryConfigurationService.Salt;

        // There should be no change on April 30th or even on May the 4th because the first Monday is the 5th.
        this.Time.Set( new DateTime( 2025, 4, 30, 0, 0, 0, DateTimeKind.Utc ) );
        Assert.Equal( initialSalt, this.TelemetryConfigurationService.Salt );

        this.Time.Set( new DateTime( 2025, 5, 4, 0, 0, 0, DateTimeKind.Utc ) );
        Assert.Equal( initialSalt, this.TelemetryConfigurationService.Salt );

        // Now there should be a change.
        this.Time.Set( new DateTime( 2025, 5, 5, 0, 0, 0, DateTimeKind.Utc ) );
        Assert.NotEqual( initialSalt, this.TelemetryConfigurationService.Salt );
    }
}