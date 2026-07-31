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

public sealed class TelemetryUploaderTests : TestsBase, IDisposable
{
    private const string _feedbackDirectory = @"C:\feedback";

    private readonly ITelemetryUploader _uploader;

    // Registered for every test in this class. A sync point that no test has enabled does not block, so this is inert
    // except in the test that drives the shutdown race. See #1764.
    private readonly TestSynchronizationProvider _synchronizationProvider = new();

    public TelemetryUploaderTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } )
    {
        this.FileSystem.CreateDirectory( _feedbackDirectory );
        this._uploader = this.ServiceProvider.GetRequiredBackstageService<ITelemetryUploader>();

        this.TelemetryConfigurationService.SetConsent( TelemetryConsent.Yes );

        // Activation is lazy (#1701): it no longer happens at Initialize, so seed the device id / salts / upload timing
        // explicitly. These tests exercise an active telemetry session, and the upload-throttle tests in particular
        // depend on LastUploadTime being seeded (as it was when Initialize used to activate).
        this.TelemetryConfigurationService.EnsureActivated();
    }

    /// <summary>
    /// Releases every sync point, so that a test that failed while the code under test was blocked cannot hang the run.
    /// </summary>
    public void Dispose() => this._synchronizationProvider.Dispose();

    protected override void ConfigureServices( ServiceProviderBuilder services )
    {
        services.AddTelemetryServices();
        services.AddTools();
        services.AddSingleton<ITestSynchronizationProvider>( this._synchronizationProvider );
    }

    protected override void OnAfterServicesCreated( Services services )
    {
        base.OnAfterServicesCreated( services );
        services.HttpClientFactory.InsertHook( r => r.RequestUri!.Host == "bits.postsharp.net", this.ProcessBitsRequest );
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
        var usageReporter = this.ServiceProvider.GetRequiredBackstageService<IUsageSessionFactory>();
        var session = usageReporter.CreateSession( "TestUsage" );
        session.Dispose();

        await this.AssertUploadedAsync( true );
    }

    // Captures an exception report the same way the telemetry context does in production — resolving the effective
    // action from the configuration and invoking the capturer. See #1701.
    private void CaptureException( Exception exception, ExceptionReportingKind kind = ExceptionReportingKind.Exception )
    {
        var scenario = kind == ExceptionReportingKind.Exception ? TelemetryScenario.Exception : TelemetryScenario.Performance;
        var action = this.ServiceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>().GetEffectiveConsent( scenario );

        this.ServiceProvider.GetRequiredBackstageService<IExceptionCapturer>()
            .Capture( ExceptionClassifier.Classify( exception ), kind, action, writeLocalReport: true, adapter: null );
    }

    [Fact]
    public async Task ExceptionsAreUploaded()
    {
        this.TelemetryConfigurationService.SetConsent( TelemetryConsent.Yes );

        this.CaptureException( new InvalidOperationException( "Test Exception" ) );

        await this.AssertUploadedAsync( true );
    }

    [Fact]
    public async Task PerformanceProblemsAreUploaded()
    {
        this.TelemetryConfigurationService.SetConsent( TelemetryConsent.Yes );

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

        this.TelemetryConfigurationService.SetConsent( TelemetryConsent.Yes );

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

    [Fact]
    public async Task BackstageWorkerIsStartedWhenTheProcessShutsDownDuringStartUpload()
    {
        // #1764: reproduces the interleaving that stopped every telemetry upload. StartUpload normally runs as a
        // background task (BackstageServicesInitializer enqueues it), and it enqueues the start of the upload process.
        // When the process began shutting down between the two, that nested enqueue was refused, the exception was
        // raised inside a task nobody observes, and the upload process was silently never started.
        //
        // The sync point makes the interleaving deterministic instead of relying on timing: we hold StartUpload just
        // before it enqueues, begin the shutdown, and only then let it continue.
        this.Time.AddTime( TimeSpan.FromMinutes( 20 ) );

        this._synchronizationProvider.EnableSyncPoint( TelemetryUploader.BeforeEnqueueUploadSyncPoint );

        var startUploadResult = false;

        try
        {
            // Start the upload the way the initializer does: as a background task, so the enqueue inside it is nested.
            var startUpload = this.BackgroundTasks.Enqueue( () => startUploadResult = this._uploader.StartUpload() );

            await this._synchronizationProvider.WaitForSyncPointReachedAsync( TelemetryUploader.BeforeEnqueueUploadSyncPoint );

            // The process starts shutting down while StartUpload is still running, exactly as ShutdownService does on
            // ProcessExit. This must not prevent the upload process from being started.
            var completion = this.BackgroundTasks.CompleteAsync();

            this._synchronizationProvider.ReleaseSyncPoint( TelemetryUploader.BeforeEnqueueUploadSyncPoint );

            await startUpload;
            await completion;
        }
        finally
        {
            // Never leave a thread blocked on a sync point, however the assertions go.
            this._synchronizationProvider.ReleaseAll();
        }

        Assert.True( startUploadResult );
        Assert.Single( this.ProcessExecutor.StartedProcesses );
    }
}