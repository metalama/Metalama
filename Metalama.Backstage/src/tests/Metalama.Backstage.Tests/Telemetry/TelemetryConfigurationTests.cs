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

    [Theory]
    [InlineData( true, true )]
    [InlineData( false, false )]
    public void SetStatus( bool input, bool output )
    {
        this.TelemetryConfigurationService.SetStatus( input );
        Assert.Equal( output, this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Usage ) );
    }

    [Theory]
    [InlineData( true, true )]
    [InlineData( false, false )]
    public void SetStatusAffectsRss( bool input, bool output )
    {
        // The in-product opt-out (SetStatus) must stop the RSS feed fetch, just like the
        // METALAMA_TELEMETRY_OPT_OUT environment variable does. See issue #1670.
        this.TelemetryConfigurationService.SetStatus( input );
        Assert.Equal( output, this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Rss ) );
    }

    [Fact]
    public void UsageReportingEnabledByDefault()
    {
        // Usage reporting is opt-out: enabled by default.
        Assert.True( this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Usage ) );
        Assert.True( this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Rss ) );
    }

    [Fact]
    public void ExceptionReportingDisabledByDefault()
    {
        // Exception and performance-problem reporting are opt-in: disabled until the user explicitly enables them.
        Assert.False( this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Exception ) );
        Assert.False( this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Performance ) );
    }

    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void SetStatusPerScenarioIsIndependent( bool enabled )
    {
        // Start from a known opposite state.
        this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Usage, !enabled );
        this.TelemetryConfigurationService.SetStatus( TelemetryScenario.Exception, enabled );

        Assert.Equal( !enabled, this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Usage ) );
        Assert.Equal( enabled, this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Exception ) );
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
        Assert.Equal( isEnabled, this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Rss ) );
    }

    [Fact]
    public void SaltRotation()
    {
        this.Time.Set( new DateTime( 2025, 4, 10, 0, 0, 0, DateTimeKind.Utc ) );
        this.TelemetryConfigurationService.Initialize();
        var initialSalt = this.TelemetryConfigurationService.MatomoSalt;

        // There should be no change on April 30th or even on May the 4th because the first Monday is the 5th.
        this.Time.Set( new DateTime( 2025, 4, 30, 0, 0, 0, DateTimeKind.Utc ) );
        Assert.Equal( initialSalt, this.TelemetryConfigurationService.MatomoSalt );

        this.Time.Set( new DateTime( 2025, 5, 4, 0, 0, 0, DateTimeKind.Utc ) );
        Assert.Equal( initialSalt, this.TelemetryConfigurationService.MatomoSalt );

        // Now there should be a change.
        this.Time.Set( new DateTime( 2025, 5, 5, 0, 0, 0, DateTimeKind.Utc ) );
        Assert.NotEqual( initialSalt, this.TelemetryConfigurationService.MatomoSalt );
    }

    [Theory]
    [InlineData( false )]
    [InlineData( true )]
    public void DiagnosticSaltRotation( bool exceptionReporting )
    {
        // The first-party-only diagnostic salts (#1668) must rotate on the same monthly cadence as MatomoSalt/DeviceId.
        long GetSalt()
            => exceptionReporting
                ? this.TelemetryConfigurationService.ExceptionReportingSalt
                : this.TelemetryConfigurationService.UsageTrackingSalt;

        this.Time.Set( new DateTime( 2025, 4, 10, 0, 0, 0, DateTimeKind.Utc ) );
        this.TelemetryConfigurationService.Initialize();
        var initialDiagnosticSalt = GetSalt();

        // No rotation before the first Monday of May (the 5th).
        this.Time.Set( new DateTime( 2025, 4, 30, 0, 0, 0, DateTimeKind.Utc ) );
        Assert.Equal( initialDiagnosticSalt, GetSalt() );

        this.Time.Set( new DateTime( 2025, 5, 4, 0, 0, 0, DateTimeKind.Utc ) );
        Assert.Equal( initialDiagnosticSalt, GetSalt() );

        // Now there should be a change, in lockstep with MatomoSalt.
        this.Time.Set( new DateTime( 2025, 5, 5, 0, 0, 0, DateTimeKind.Utc ) );
        Assert.NotEqual( initialDiagnosticSalt, GetSalt() );
    }

    [Fact]
    public void SaltsAreGeneratedAndMutuallyDistinct()
    {
        // Each salt must be a non-zero value, and the three salts must be mutually distinct, so that the Matomo,
        // usage-tracking and exception-reporting pseudonyms cannot be correlated with one another (#1668).
        this.Time.Set( new DateTime( 2025, 4, 10, 0, 0, 0, DateTimeKind.Utc ) );
        this.TelemetryConfigurationService.Initialize();

        var matomoSalt = this.TelemetryConfigurationService.MatomoSalt;
        var usageTrackingSalt = this.TelemetryConfigurationService.UsageTrackingSalt;
        var exceptionReportingSalt = this.TelemetryConfigurationService.ExceptionReportingSalt;

        Assert.NotEqual( 0, matomoSalt );
        Assert.NotEqual( 0, usageTrackingSalt );
        Assert.NotEqual( 0, exceptionReportingSalt );

        Assert.NotEqual( matomoSalt, usageTrackingSalt );
        Assert.NotEqual( matomoSalt, exceptionReportingSalt );
        Assert.NotEqual( usageTrackingSalt, exceptionReportingSalt );
    }

    [Fact]
    public void DeviceHashesAreMutuallyUncorrelatable()
    {
        // For one device, the hash sent to Matomo (keyed by MatomoSalt) and the two first-party hashes — the
        // usage-tracking <Machine> (keyed by UsageTrackingSalt) and the exception-reporting <ClientId> (keyed by
        // ExceptionReportingSalt) — must all differ, so none of the three datasets shares a join key. #1668.
        this.Time.Set( new DateTime( 2025, 4, 10, 0, 0, 0, DateTimeKind.Utc ) );
        this.TelemetryConfigurationService.Initialize();

        var deviceId = this.TelemetryConfigurationService.DeviceId.ToString();
        var matomoHash = HashUtilities.ComputeInt64Hmac( deviceId, this.TelemetryConfigurationService.MatomoSalt );
        var usageTrackingHash = HashUtilities.ComputeInt64Hmac( deviceId, this.TelemetryConfigurationService.UsageTrackingSalt );
        var exceptionReportingHash = HashUtilities.ComputeInt64Hmac( deviceId, this.TelemetryConfigurationService.ExceptionReportingSalt );

        Assert.NotEqual( matomoHash, usageTrackingHash );
        Assert.NotEqual( matomoHash, exceptionReportingHash );
        Assert.NotEqual( usageTrackingHash, exceptionReportingHash );
    }

    [Theory]
    [InlineData( "matomo" )]
    [InlineData( "usage" )]
    [InlineData( "exception" )]
    public void SaltDoesNotDependOnSeededRandomNumberGenerator( string saltKind )
    {
        // The test RNG service (Metalama.Backstage.Infrastructure.RandomNumberGenerator) is seeded
        // deterministically (seed 0, see TestsBase). Each anonymization salt is a security-sensitive value
        // (the HMAC-SHA256 key for the device/licensee hashes) and must be generated by a CSPRNG (#1654), so it
        // must NOT be reproducible from the seeded RNG service. Two independent service providers sharing the
        // same RNG seed must therefore produce different salts.
        long GenerateSalt()
        {
            var serviceProvider = this.CloneServiceCollection().BuildServiceProvider();
            var telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
            telemetryConfigurationService.Initialize();

            return saltKind switch
            {
                "matomo" => telemetryConfigurationService.MatomoSalt,
                "usage" => telemetryConfigurationService.UsageTrackingSalt,
                "exception" => telemetryConfigurationService.ExceptionReportingSalt,
                _ => throw new ArgumentOutOfRangeException( nameof(saltKind) )
            };
        }

        var salt1 = GenerateSalt();
        var salt2 = GenerateSalt();

        Assert.NotEqual( salt1, salt2 );
    }
}