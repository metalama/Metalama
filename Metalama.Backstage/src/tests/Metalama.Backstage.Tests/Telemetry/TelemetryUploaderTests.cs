// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Tools;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Telemetry;

public sealed class TelemetryUploaderTests : TestsBase
{
    private const string _feedbackDirectory = @"C:\feedback";

    private readonly ITelemetryUploader _uploader;

    public TelemetryUploaderTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } )
    {
        this.FileSystem.CreateDirectory( _feedbackDirectory );
        this._uploader = this.ServiceProvider.GetRequiredBackstageService<ITelemetryUploader>();

        this.TelemetryConfigurationService.SetStatus( true );

        // Activation is lazy (#1701): it no longer happens at Initialize, so seed the device id / salts / upload timing
        // explicitly. These tests exercise an active telemetry session, and the upload-throttle tests in particular
        // depend on LastUploadTime being seeded (as it was when Initialize used to activate).
        this.TelemetryConfigurationService.EnsureActivated();
    }

    protected override void ConfigureServices( ServiceProviderBuilder services )
    {
        services.AddTelemetryServices();
        services.AddTools();
    }

    protected override void OnAfterServicesCreated( Services services )
    {
        base.OnAfterServicesCreated( services );
        services.HttpClientFactory.AddHook( r => r.RequestUri!.Host == "bits.postsharp.net", this.ProcessBitsRequest );
    }

    private async Task<HttpResponseMessage> ProcessBitsRequest( HttpRequestMessage requestMessage, CancellationToken cancellationToken )
    {
        var content = ((MultipartFormDataContent) requestMessage.Content!).Single();

        // Read the filename from the content headers
        var fileName = content.Headers.ContentDisposition?.FileName ?? string.Empty;

        // ReSharper disable once UseAwaitUsing
        using ( var outputFile = this.FileSystem.Open(
                   Path.Combine( _feedbackDirectory, fileName ),
                   FileMode.Create,
                   FileAccess.Write,
                   FileShare.None,
                   4096,
                   FileOptions.Asynchronous ) )
        {
            // ReSharper disable once MethodSupportsCancellation
            await content.CopyToAsync( outputFile );
        }

        return new HttpResponseMessage( HttpStatusCode.Accepted );
    }

    private async Task AssertUploadedAsync( bool uploadedFileExpected )
    {
        await this._uploader.UploadAsync();

        var processedRequests = this.HttpClientFactory.ProcessedRequests;
        var uploadedFiles = this.FileSystem.EnumerateFiles( _feedbackDirectory, "*.psf" );

        if ( uploadedFileExpected )
        {
            Assert.Single( processedRequests, x => x.Request.RequestUri!.ToString().ContainsOrdinal( "bits.postsharp.net" ) );

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
        session.Dispose();

        await this.AssertUploadedAsync( true );
    }

    // Captures an exception report the same way the telemetry context does in production — resolving the effective
    // action from the configuration and invoking the capturer. See #1701.
    private void CaptureException( Exception exception, ExceptionReportingKind kind = ExceptionReportingKind.Exception )
    {
        var scenario = kind == ExceptionReportingKind.Exception ? TelemetryScenario.Exception : TelemetryScenario.Performance;
        var action = this.ServiceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>().GetEffectiveReportingAction( scenario );

        this.ServiceProvider.GetRequiredBackstageService<IExceptionCapturer>()
            .Capture( ExceptionClassifier.Classify( exception ), kind, action, writeLocalReport: true, adapter: null );
    }

    [Fact]
    public async Task ExceptionsAreUploaded()
    {
        this.TelemetryConfigurationService.SetStatus( true );

        this.CaptureException( new InvalidOperationException( "Test Exception" ) );

        await this.AssertUploadedAsync( true );
    }

    [Fact]
    public async Task PerformanceProblemsAreUploaded()
    {
        this.TelemetryConfigurationService.SetStatus( true );

        this.CaptureException( new InvalidOperationException( "Test Performance Problem" ), ExceptionReportingKind.PerformanceProblem );

        await this.AssertUploadedAsync( true );
    }

    [Fact]
    public async Task BackstageWorkerIsStartedAfter20Minutes()
    {
        // Advance the time because the telemetry uploader does not upload data for the first 15 minutes after initial execution.
        this.Time.AddTime( TimeSpan.FromMinutes( 20 ) );

        Assert.True( this._uploader.StartUpload() );

        await this.BackgroundTasks.WhenNoPendingTaskAsync();

        Assert.Single( this.ProcessExecutor.StartedProcesses );

        var platformInfo = this.ServiceProvider.GetRequiredBackstageService<IPlatformInfo>();
        var expectedExecutedFileName = platformInfo.DotNetExePath;

        Assert.Equal( expectedExecutedFileName, this.ProcessExecutor.StartedProcesses[0].FileName );
    }

    [Fact]
    public async Task PackageAndQueueFilesAreDeletedAfterSuccessfulUpload()
    {
        var standardDirectories = this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>();

        this.TelemetryConfigurationService.SetStatus( true );

        // Queue an exception report so that there is something to upload.
        this.CaptureException( new InvalidOperationException( "Test Exception" ) );

        await this._uploader.UploadAsync();

        // The upload must have succeeded.
        Assert.Single( this.HttpClientFactory.ProcessedRequests, x => x.Request.RequestUri!.ToString().ContainsOrdinal( "bits.postsharp.net" ) );

        // The local .psf package has no review value and must be deleted immediately after a successful upload.
        if ( this.FileSystem.DirectoryExists( standardDirectories.TelemetryUploadPackagesDirectory ) )
        {
            Assert.Empty( this.FileSystem.EnumerateFiles( standardDirectories.TelemetryUploadPackagesDirectory, "*.psf" ) );
        }

        // The queued files that were sent must be deleted (the post-upload deletion path executes).
        if ( this.FileSystem.DirectoryExists( standardDirectories.TelemetryUploadQueueDirectory ) )
        {
            Assert.Empty( this.FileSystem.GetFiles( standardDirectories.TelemetryUploadQueueDirectory ) );
        }
    }

    [Fact]
    public void BackstageWorkerIsNotStartedAfter10Minutes()
    {
        this.Time.AddTime( TimeSpan.FromMinutes( 10 ) );

        Assert.False( this._uploader.StartUpload() );

        Assert.Empty( this.ProcessExecutor.StartedProcesses );
    }
}