// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Telemetry;

// Covers the combinations of the repository's metalama.json and the user's (global) telemetry.json, as resolved by
// ITelemetryService.OpenContext. The critical guarantee is that a repository-wide opt-out performs and activates no
// telemetry whatsoever. See #1701.
public sealed class TelemetryContextTests : TestsBase
{
    private const string _repoRoot = @"C:\repo";
    private const string _projectDirectory = @"C:\repo\src\project";

    public TelemetryContextTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo { IsTelemetryEnabled = true } ) { }

    protected override void ConfigureServices( ServiceProviderBuilder services ) => services.AddTelemetryServices();

    private ITelemetryService TelemetryService => this.ServiceProvider.GetRequiredBackstageService<ITelemetryService>();

    // The device identifier is created only when telemetry is activated; it stays null while telemetry is dormant.
    private Guid? DeviceId => this.ConfigurationManager!.Get<TelemetryConfiguration>().DeviceId;

    private void CreateRepository( bool? telemetryEnabled )
    {
        this.FileSystem.CreateDirectory( Path.Combine( _repoRoot, ".git" ) );

        if ( telemetryEnabled != null )
        {
            this.FileSystem.WriteAllText(
                Path.Combine( _repoRoot, "metalama.json" ),
                $$"""{ "telemetry": { "enabled": {{(telemetryEnabled.Value ? "true" : "false")}} } }""" );
        }
    }

    [Fact]
    public void NoMetalamaJsonAndGlobalDefault_UsageEnabled()
    {
        this.CreateRepository( telemetryEnabled: null );

        Assert.True( this.TelemetryService.OpenContext( _projectDirectory ).IsTelemetryEnabled( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void RepositoryOptInAndGlobalDefault_UsageEnabled()
    {
        this.CreateRepository( telemetryEnabled: true );

        Assert.True( this.TelemetryService.OpenContext( _projectDirectory ).IsTelemetryEnabled( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void RepositoryOptOut_AllScenariosDisabled()
    {
        this.CreateRepository( telemetryEnabled: false );

        var context = this.TelemetryService.OpenContext( _projectDirectory );

        Assert.False( context.IsTelemetryEnabled( TelemetryScenario.Usage ) );
        Assert.False( context.IsTelemetryEnabled( TelemetryScenario.Exception ) );
        Assert.False( context.IsTelemetryEnabled( TelemetryScenario.Performance ) );
    }

    [Fact]
    public void RepositoryOptOut_PerformsNoTelemetryAndNeverActivates()
    {
        // This is the critical guarantee: a repository-wide opt-out collects/sends no telemetry and never even
        // activates telemetry (no device identifier is ever written), regardless of the global telemetry.json.
        this.CreateRepository( telemetryEnabled: false );

        var context = this.TelemetryService.OpenContext( _projectDirectory );

        using ( var session = context.StartUsageSession( "TestUsage", "Project1" ) )
        {
            Assert.False( session.ShouldCollectMetrics );
        }

        context.ReportException( new InvalidOperationException( "test" ) );

        Assert.Null( this.DeviceId );
    }

    [Fact]
    public void NonOptedOutRepository_UsageSessionActivatesTelemetry()
    {
        // The contrast to the opt-out case: a non-opted-out usage session does activate telemetry (lazily).
        this.CreateRepository( telemetryEnabled: null );

        Assert.Null( this.DeviceId );

        var context = this.TelemetryService.OpenContext( _projectDirectory );

        using ( var session = context.StartUsageSession( "TestUsage", "Project1" ) )
        {
            Assert.True( session.ShouldCollectMetrics );
        }

        Assert.NotNull( this.DeviceId );
    }

    [Fact]
    public void RepositoryOptIn_DoesNotOverrideEnvironmentVariableOptOut()
    {
        // The environment variable keeps absolute priority: an explicit repository opt-in cannot re-enable telemetry.
        this.EnvironmentVariableProvider.Environment[Backstage.Telemetry.TelemetryConfigurationService.OptOutEnvironmentVariable] = "1";
        this.CreateRepository( telemetryEnabled: true );

        Assert.False( this.TelemetryService.OpenContext( _projectDirectory ).IsTelemetryEnabled( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void InProductOptOut_DisablesUsageWithoutMetalamaJson()
    {
        // `metalama telemetry disable` (SetStatus(false)) disables usage even when there is no metalama.json.
        this.CreateRepository( telemetryEnabled: null );
        this.TelemetryConfigurationService.SetStatus( false );

        Assert.False( this.TelemetryService.OpenContext( _projectDirectory ).IsTelemetryEnabled( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void NullContext_AllScenariosDisabled()
    {
        var context = this.TelemetryService.NullContext;

        Assert.False( context.IsTelemetryEnabled( TelemetryScenario.Usage ) );
        Assert.False( context.IsTelemetryEnabled( TelemetryScenario.Exception ) );
        Assert.False( context.IsTelemetryEnabled( TelemetryScenario.Performance ) );
    }
}
