// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Audit;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Telemetry.Metrics;
using Metalama.Backstage.Testing;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Telemetry;

public sealed class UsageReporterTests : TestsBase
{
    // The salt is generated from a CSPRNG (see #1654), so it can no longer be derived from the deterministically-seeded
    // test RNG. We pin the device id and salt so that the device hash sent to Matomo stays deterministic.
    private static readonly Guid _testDeviceId = new( "d8e7f6a5-b4c3-2d1e-0f9a-8b7c6d5e4f3a" );
    private const long _testSalt = 0x0123456789ABCDEF;

    // This field can be modified by tests before the first use of the service provider.
    private TestApplicationInfo _applicationInfo = new() { IsTelemetryEnabled = true };

    public UsageReporterTests( ITestOutputHelper logger ) : base( logger ) { }

    protected override void OnAfterServicesCreated( Services services )
    {
        base.OnAfterServicesCreated( services );

        services.ConfigurationManager!.Update<TelemetryConfiguration>(
            c => c with
            {
                UsageReportingAction = ReportingAction.Yes,
                DeviceId = _testDeviceId,
                MatomoSalt = _testSalt,
                LastSaltChangeTime = this.Time.UtcNow
            } );
    }

    protected override void ConfigureServices( ServiceProviderBuilder services )
        => services
            .AddSingleton<IApplicationInfoProvider>( new ApplicationInfoProvider( this._applicationInfo ) )
            .AddSingleton( serviceProvider => new TelemetryLogger( serviceProvider ) )
            .AddSingleton<ITelemetryUploader>( new NullTelemetryUploader() )
            .AddSingleton<TelemetryReportUploader>( serviceProvider => new TelemetryReportUploader( serviceProvider ) )
            .AddSingleton( serviceProvider => new MatomoUploader( serviceProvider ) );

    private void ReportSession( string kind = "TestSession" )
    {
        var reporter = new UsageReporter( this.ServiceProvider );
        var session = reporter.StartSession( kind );
        Assert.NotNull( session );
        Assert.NotEmpty( session.Metrics );

        session.Dispose();

        Assert.True( session.Metrics.IsReadOnly );
        Assert.Single( this.FileSystem.Mock.AllFiles, f => Path.GetFileName( f ).StartsWith( "Usage-", StringComparison.Ordinal ) );
        Assert.Single( this.FileSystem.Mock.AllFiles, f => Path.GetFileName( f ).StartsWith( "Telemetry-", StringComparison.Ordinal ) );
        Assert.Equal( 2, this.FileSystem.Mock.AllFiles.Count() );
    }

    private void AssertReportingDisabled()
    {
        // We can't use the reporter from the constructor, because it's been created with the wrong configuration.
        var reporter = new UsageReporter( this.ServiceProvider );

        var session = reporter.StartSession( "TestSession", "TestProject" );
        Assert.False( session.ShouldCollectMetrics );
        session.Dispose();
        this.BackgroundTasks.WhenNoPendingTaskAsync().Wait();
        Assert.Empty( this.FileSystem.Mock.AllFiles );
        Assert.Empty( this.HttpClientFactory.ProcessedRequests );
    }

    [Theory]
    [InlineData( ReportingAction.Yes, true )]
    [InlineData( ReportingAction.No, false )]
    [InlineData( ReportingAction.Default, true )]
    public void UsageIsReportedAsConfiguredWhenTelemetryIsEnabled( ReportingAction usageReportingAction, bool shouldReport )
    {
        this.ConfigurationManager!.Update<TelemetryConfiguration>( c => c with { UsageReportingAction = usageReportingAction } );

        if ( shouldReport )
        {
            this.ReportSession();
        }
        else
        {
            this.AssertReportingDisabled();
        }
    }

    [Fact]
    public void UsageIsNotReportedWhenTelemetryIsDisabled()
    {
        this._applicationInfo = new TestApplicationInfo() { IsTelemetryEnabled = false };
        this.AssertReportingDisabled();
    }

    [Fact]
    public void UsageIsNotReportedWhenOptOutEnvironmentVariableIsSet()
    {
        this.EnvironmentVariableProvider.Environment["METALAMA_TELEMETRY_OPT_OUT"] = "true";
        this.AssertReportingDisabled();
    }

    [Fact]
    public void UsageIsNotReportedForUnattendedBuild()
    {
        this._applicationInfo = new TestApplicationInfo() { IsUnattendedProcess = true };

        this.AssertReportingDisabled();
    }

    private void AssertSessionShouldBeReported( string projectName = "TestProject" )
    {
        var reporter = new UsageReporter( this.ServiceProvider );
        var session = reporter.StartSession( "Usage", projectName );
        Assert.NotNull( session );
        Assert.True( session.ShouldCollectMetrics );
    }

    private void AssertSessionShouldNotBeReported( string projectName = "TestProject" )
    {
        var reporter = new UsageReporter( this.ServiceProvider );
        var session = reporter.StartSession( "Usage", projectName );
        Assert.NotNull( session );
        Assert.False( session.ShouldCollectMetrics );
    }

    [Fact]
    public void FirstSessionShouldBeReported()
    {
        this.AssertSessionShouldBeReported();
    }

    [Fact]
    public void SessionShouldNotBeReportedWhenReportedRecently()
    {
        this.AssertSessionShouldBeReported();
        this.AssertSessionShouldNotBeReported();
        this.Time.AddTime( TimeSpan.FromDays( 1 ).Add( -TimeSpan.FromMinutes( 1 ) ) );
        this.AssertSessionShouldNotBeReported();
    }

    [Fact]
    public void SessionShouldBeReportedAfterOneDay()
    {
        this.AssertSessionShouldBeReported();
        this.Time.AddTime( TimeSpan.FromDays( 1 ) );
        this.AssertSessionShouldBeReported();
        this.Time.AddTime( TimeSpan.FromDays( 1 ) );
        this.AssertSessionShouldBeReported();
    }

    [Fact]
    public void SessionShouldBeReportedAfterOneDayEvenWhenOtherProjectsReported()
    {
        this.AssertSessionShouldBeReported( "TestProject1" );
        this.AssertSessionShouldNotBeReported( "TestProject1" );
        this.AssertSessionShouldBeReported( "TestProject2" );
        this.AssertSessionShouldNotBeReported( "TestProject2" );
        this.Time.AddTime( TimeSpan.FromDays( 1 ) );
        this.AssertSessionShouldBeReported( "TestProject1" );
        this.AssertSessionShouldNotBeReported( "TestProject1" );
        this.AssertSessionShouldBeReported( "TestProject2" );
        this.AssertSessionShouldNotBeReported( "TestProject2" );
    }

    [Fact]
    public void SessionShouldBeCleanedUpAfterOneDay()
    {
        void AssertSessionsCount( int count ) => Assert.Equal( count, this.ConfigurationManager!.Get<TelemetryConfiguration>().Sessions.Count );

        AssertSessionsCount( 0 );
        this.AssertSessionShouldBeReported( "TestProject1" );
        AssertSessionsCount( 1 );
        this.AssertSessionShouldBeReported( "TestProject2" );
        AssertSessionsCount( 2 );
        this.Time.AddTime( TimeSpan.FromDays( 1 ) );
        this.AssertSessionShouldBeReported( "TestProject1" );
        AssertSessionsCount( 1 );
    }

    [Fact]
    public async Task SessionsCanBeReportedConcurrentlyAsync()
    {
        var event1 = new SemaphoreSlim( 0 );
        var event2 = new SemaphoreSlim( 0 );

        async Task<IDisposable> StartSession( string projectName, SemaphoreSlim e )
        {
            var reporter = new UsageReporter( this.ServiceProvider );
            var session = reporter.StartSession( "TestSession", projectName );
            Assert.NotNull( session );
            session.Metrics.Add( new StringMetric( "ProjectName", projectName ) );

            await e.WaitAsync();

            Assert.Single( session.Metrics, m => m is StringMetric stringMetric && stringMetric.Value == projectName );

            return session;
        }

        var session1Task = StartSession( "TestProject1", event1 );
        var session2Task = StartSession( "TestProject2", event2 );

        event1.Release();
        await session1Task;

        event2.Release();
        await session2Task;

        (await session1Task).Dispose();
        (await session2Task).Dispose();

        Assert.Equal( 2, this.FileSystem.Mock.AllFiles.Count( f => Path.GetFileName( f ).StartsWith( "Usage-", StringComparison.Ordinal ) ) );
        Assert.Equal( 1, this.FileSystem.Mock.AllFiles.Count( f => Path.GetFileName( f ).StartsWith( "Telemetry-", StringComparison.Ordinal ) ) );
        Assert.Equal( 3, this.FileSystem.Mock.AllFiles.Count() );
    }

    [Fact]
    public async Task SessionReportedToMatomoAsync()
    {
        async Task StartSessionAndAssert( string projectName, bool isReportingExpected, string? random, DeviceAgeBucket expectedDeviceAge )
        {
            var reporter = new UsageReporter( this.ServiceProvider );

            this.HttpClientFactory.ClearProcessedRequests();

            var session = reporter.StartSession( "TestSession", projectName );
            session.Dispose();
            await this.BackgroundTasks.WhenNoPendingTaskAsync();

            if ( isReportingExpected )
            {
                var (matomoRequest, _) = Assert.Single( this.HttpClientFactory.ProcessedRequests, r => r.Request.RequestUri?.Host == "postsharp.matomo.cloud" );
                var matomoRequestUri = matomoRequest.RequestUri?.ToString();

                this.Logger.WriteLine( matomoRequestUri );

                Assert.Equal(
                    $"https://postsharp.matomo.cloud/matomo.php?idsite=6&rec=1&action_name=usage&_id=633a82166c05736f&uid=633a82166c05736f&dimension3=Metalama&dimension4=0.0&dimension5={expectedDeviceAge}&new_visit=1&rand={random}",
                    matomoRequestUri );
            }
            else
            {
                Assert.Empty( this.HttpClientFactory.ProcessedRequests );
            }
        }

        // First session must cause audit. (The 'rand' Matomo cache-buster is drawn from the seeded test RNG; its value
        // shifted with lazy activation (#1701) because the device-id draw now happens on first report. The device hash
        // '_id', the once-per-day cadence and the device-age buckets are unchanged.)
        await StartSessionAndAssert( "Project1", true, "5cf58a1a689e1e0c", DeviceAgeBucket.LessThan1 );

        // Second session (even with different project) must not cause audit.
        await StartSessionAndAssert( "Project1", false, null, DeviceAgeBucket.LessThan1 );
        await StartSessionAndAssert( "Project2", false, null, DeviceAgeBucket.LessThan1 );

        // Third session the next day must cause audit.
        this.Time.AddTime( TimeSpan.FromDays( 1.1 ) );
        await StartSessionAndAssert( "Project1", true, "624e91464771d36f", DeviceAgeBucket.From1To30 );
        await StartSessionAndAssert( "Project2", false, null, DeviceAgeBucket.From1To30 );
    }
}