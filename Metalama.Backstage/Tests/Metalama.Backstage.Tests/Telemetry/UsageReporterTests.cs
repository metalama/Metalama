// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

public class UsageReporterTests : TestsBase
{
    // This field can be modified by tests before the first use of the service provider.
    private TestApplicationInfo _applicationInfo = new() { IsTelemetryEnabled = true };

    public UsageReporterTests( ITestOutputHelper logger ) : base( logger ) { }

    protected override void OnAfterServicesCreated( Services services )
    {
        services.ConfigurationManager!.Update<TelemetryConfiguration>( c => c with { UsageReportingAction = ReportingAction.Yes } );
    }

    protected override void ConfigureServices( ServiceProviderBuilder services )
        => services
            .AddSingleton<IApplicationInfoProvider>( new ApplicationInfoProvider( this._applicationInfo ) )
            .AddSingleton( serviceProvider => new TelemetryLogger( serviceProvider ) )
            .AddSingleton<ITelemetryUploader>( new NullTelemetryUploader() )
            .AddSingleton<TelemetryReportUploader>( serviceProvider => new TelemetryReportUploader( serviceProvider ) );

    private void ReportSession( string kind = "TestSession" )
    {
        var reporter = new UsageReporter( this.ServiceProvider );
        var session = reporter.StartSession( kind );
        Assert.NotNull( session );
        Assert.NotEmpty( session.Metrics );

        session.Dispose();

        Assert.Throws<InvalidOperationException>( () => session.Metrics );
        Assert.Single( this.FileSystem.Mock.AllFiles, f => Path.GetFileName( f ).StartsWith( "Usage-", StringComparison.Ordinal ) );
        Assert.Single( this.FileSystem.Mock.AllFiles, f => Path.GetFileName( f ).StartsWith( "Telemetry-", StringComparison.Ordinal ) );
        Assert.Equal( 2, this.FileSystem.Mock.AllFiles.Count() );
    }

    private void AssertReportingDisabled()
    {
        // We can't use the reporter from the constructor, because it's been created with the wrong configuration.
        var reporter = new UsageReporter( this.ServiceProvider );

        Assert.False( reporter.ShouldReportSession( "TestProject" ) );

        Assert.Null( reporter.StartSession( "TestSession" ) );
        Assert.Empty( this.FileSystem.Mock.AllFiles );
    }

    [Theory]
    [InlineData( ReportingAction.Yes, true )]
    [InlineData( ReportingAction.No, false )]
    [InlineData( ReportingAction.Ask, false )]
    public void UsageIsReportedAsConfiguredWhenTelemetryIsEnabled( ReportingAction usageReportingAction, bool shoudlReport )
    {
        this.ConfigurationManager!.Update<TelemetryConfiguration>( c => c with { UsageReportingAction = usageReportingAction } );

        if ( shoudlReport )
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

    [Fact]
    public void UsageRepostingCanBeRepeatedWithoutShouldReportSessionCheck()
    {
        this.ReportSession();
        this.FileSystem.Mock.AllFiles.ToList().ForEach( this.FileSystem.DeleteFile );
        this.ReportSession();
    }

    private void AssertSessionShouldBeReported( string projectName = "TestProject" )
    {
        var reporter = new UsageReporter( this.ServiceProvider );
        Assert.True( reporter.ShouldReportSession( projectName ) );
    }

    private void AssertSessionShouldNotBeReported( string projectName = "TestProject" )
    {
        var reporter = new UsageReporter( this.ServiceProvider );
        Assert.False( reporter.ShouldReportSession( projectName ) );
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
            var session = reporter.StartSession( "TestSession" );
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
}