// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Tools;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Telemetry;

public class TelemetryUploaderTests : TestsBase
{
    private const string _feedbackDirectory = @"C:\feedback";

    private readonly ITelemetryUploader _uploader;

    public TelemetryUploaderTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } )
    {
        this.FileSystem.CreateDirectory( _feedbackDirectory );
        this._uploader = this.ServiceProvider.GetRequiredBackstageService<ITelemetryUploader>();
        
        this.TelemetryConfigurationService.SetStatus( true );
    }

    protected override void ConfigureServices( ServiceProviderBuilder services )
    {
        services
            .AddSingleton<IHttpClientFactory>(
                serviceProvider =>
                    new TestHttpClientFactory( f => new TelemetryTestsPutMessageHandler( serviceProvider, _feedbackDirectory, f ) ) )
            .AddTelemetryServices();

        services.AddTools();
    }

    private async Task AssertUploadedAsync( bool uploadedFileExpected )
    {
        await this._uploader.UploadAsync();

        var processedRequests = this.HttpClientFactory.ProcessedRequests;
        var uploadedFiles = this.FileSystem.EnumerateFiles( _feedbackDirectory, "*.psf" );

        if ( uploadedFileExpected )
        {
            Assert.Single( processedRequests );

#if NET
            Assert.Single( uploadedFiles );
#endif
        }
        else
        {
            Assert.Empty( processedRequests );
            Assert.Empty( uploadedFiles );
        }
    }

    [Fact]
    public async Task ServiceNotCalledWhenNothingToUpload()
    {
        await this.AssertUploadedAsync( false );
    }

    [Fact]
    public async Task UsageIsUploaded()
    {
        var usageReporter = this.ServiceProvider.GetRequiredBackstageService<IUsageReporter>();
        var session = usageReporter.StartSession( "TestUsage" );
        session!.Dispose();

        await this.AssertUploadedAsync( true );
    }

    [Fact]
    public async Task ExceptionsAreUploaded()
    {
        this.TelemetryConfigurationService.SetStatus( true );
        
        var exceptionsReporter = this.ServiceProvider.GetRequiredBackstageService<IExceptionReporter>();
        exceptionsReporter.ReportException( new InvalidOperationException( "Test Exception" ) );

        await this.AssertUploadedAsync( true );
    }

    [Fact]
    public async Task PerformanceProblemsAreUploaded()
    {
        this.TelemetryConfigurationService.SetStatus( true );
        
        var exceptionsReporter = this.ServiceProvider.GetRequiredBackstageService<IExceptionReporter>();
        exceptionsReporter.ReportException( new InvalidOperationException( "Test Performance Problem" ), ExceptionReportingKind.PerformanceProblem );

        await this.AssertUploadedAsync( true );
    }

    [Fact]
    public void BackstageWorkerIsStarted()
    {
        this._uploader.StartUpload();

        var platformInfo = this.ServiceProvider.GetRequiredBackstageService<IPlatformInfo>();
        var expectedExecutedFileName = platformInfo.DotNetExePath;

        Assert.Single( this.ProcessExecutor.StartedProcesses );
        Assert.Equal( expectedExecutedFileName, this.ProcessExecutor.StartedProcesses[0].FileName );
    }
}