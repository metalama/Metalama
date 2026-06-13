// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Telemetry;

public sealed class TelemetryConfigurationTests : TestsBase
{
    public TelemetryConfigurationTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } ) { }

    private IInternalTelemetryConfigurationService InternalTelemetryConfigurationService
        => (IInternalTelemetryConfigurationService) this.TelemetryConfigurationService;

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

    [Fact]
    public void DiagnosticSaltRotation()
    {
        // The first-party-only DiagnosticSalt (#1668) must rotate on the same monthly cadence as Salt/DeviceId.
        this.Time.Set( new DateTime( 2025, 4, 10, 0, 0, 0, DateTimeKind.Utc ) );
        this.TelemetryConfigurationService.Initialize();
        var initialDiagnosticSalt = this.InternalTelemetryConfigurationService.DiagnosticSalt;

        // No rotation before the first Monday of May (the 5th).
        this.Time.Set( new DateTime( 2025, 4, 30, 0, 0, 0, DateTimeKind.Utc ) );
        Assert.Equal( initialDiagnosticSalt, this.InternalTelemetryConfigurationService.DiagnosticSalt );

        this.Time.Set( new DateTime( 2025, 5, 4, 0, 0, 0, DateTimeKind.Utc ) );
        Assert.Equal( initialDiagnosticSalt, this.InternalTelemetryConfigurationService.DiagnosticSalt );

        // Now there should be a change, in lockstep with Salt.
        this.Time.Set( new DateTime( 2025, 5, 5, 0, 0, 0, DateTimeKind.Utc ) );
        Assert.NotEqual( initialDiagnosticSalt, this.InternalTelemetryConfigurationService.DiagnosticSalt );
    }

    [Fact]
    public void DiagnosticSaltIsGeneratedAndDistinctFromSalt()
    {
        // The DiagnosticSalt must be a non-zero value, distinct from the Matomo Salt, so that identifiers
        // sent to the first-party diagnostic store cannot be correlated with the Matomo dataset (#1668).
        this.Time.Set( new DateTime( 2025, 4, 10, 0, 0, 0, DateTimeKind.Utc ) );
        this.TelemetryConfigurationService.Initialize();

        var salt = this.TelemetryConfigurationService.Salt;
        var diagnosticSalt = this.InternalTelemetryConfigurationService.DiagnosticSalt;

        Assert.NotEqual( 0, diagnosticSalt );
        Assert.NotEqual( salt, diagnosticSalt );
    }

    [Fact]
    public void MatomoIdAndDiagnosticHashAreUncorrelatable()
    {
        // For one device, the hash sent to Matomo (DeviceHash, keyed by Salt) must differ from the hash sent
        // to the first-party diagnostic store (InternalDeviceHash / <ClientId>, keyed by DiagnosticSalt). #1668.
        this.Time.Set( new DateTime( 2025, 4, 10, 0, 0, 0, DateTimeKind.Utc ) );
        this.TelemetryConfigurationService.Initialize();

        var deviceId = this.TelemetryConfigurationService.DeviceId.ToString();
        var matomoHash = HashUtilities.ComputeInt64Hmac( deviceId, this.TelemetryConfigurationService.Salt );
        var diagnosticHash = HashUtilities.ComputeInt64Hmac( deviceId, this.InternalTelemetryConfigurationService.DiagnosticSalt );

        Assert.NotEqual( matomoHash, diagnosticHash );
    }

    [Fact]
    public void DiagnosticSaltDoesNotDependOnSeededRandomNumberGenerator()
    {
        // Like Salt, DiagnosticSalt is a security-sensitive HMAC key and must be generated by a CSPRNG (#1654),
        // so it must NOT be reproducible from the deterministically-seeded test RNG service.
        long GenerateDiagnosticSalt()
        {
            var serviceProvider = this.CloneServiceCollection().BuildServiceProvider();
            var telemetryConfigurationService =
                (IInternalTelemetryConfigurationService) serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
            telemetryConfigurationService.Initialize();

            return telemetryConfigurationService.DiagnosticSalt;
        }

        var salt1 = GenerateDiagnosticSalt();
        var salt2 = GenerateDiagnosticSalt();

        Assert.NotEqual( salt1, salt2 );
    }

    [Fact]
    public void SaltDoesNotDependOnSeededRandomNumberGenerator()
    {
        // The test RNG service (Metalama.Backstage.Infrastructure.RandomNumberGenerator) is seeded
        // deterministically (seed 0, see TestsBase). The anonymization salt is a security-sensitive
        // value (the HMAC-SHA256 key for the device/licensee hashes) and must be generated by a
        // CSPRNG, so it must NOT be reproducible from the seeded RNG service. Two independent service
        // providers sharing the same RNG seed must therefore produce different salts.
        long GenerateSalt()
        {
            var serviceProvider = this.CloneServiceCollection().BuildServiceProvider();
            var telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
            telemetryConfigurationService.Initialize();

            return telemetryConfigurationService.Salt;
        }

        var salt1 = GenerateSalt();
        var salt2 = GenerateSalt();

        Assert.NotEqual( salt1, salt2 );
    }
}