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

/// <summary>
/// Verifies that telemetry never fails the operation it observes when the global configuration mutex is unavailable.
/// </summary>
/// <remarks>
/// <para>
/// See issue #1847. Every reported crash acquired the mutex on behalf of housekeeping, and the resulting
/// <see cref="TimeoutException"/> propagated out of the compiler or of the design-time analyzer and aborted the
/// compilation. The most damaging of them happened in <c>UsageSession.Dispose</c>, after the compilation had already
/// succeeded.
/// </para>
/// <para>
/// These tests use the real <see cref="Configuration.ConfigurationManager"/>, rather than the in-memory one, because
/// the mutex is what is being exercised. Its timeout is shortened, because the mutex is genuinely held by
/// <see cref="HeldConfigurationMutex"/> and the tests would otherwise wait for the thirty seconds of the production
/// timeout.
/// </para>
/// </remarks>
public sealed class TelemetryWithUnavailableMutexTests : TestsBase
{
    private const int _mutexTimeoutMilliseconds = 200;

    private const string _repositoryDirectory = @"C:\repo";
    private const string _projectDirectory = @"C:\repo\src\project";

    public TelemetryWithUnavailableMutexTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo { IsTelemetryEnabled = true } ) { }

    protected override void ConfigureServices( ServiceProviderBuilder services )
    {
        services.AddTelemetryServices();

        services.AddSingleton<IConfigurationManager>(
            serviceProvider => new Configuration.ConfigurationManager( serviceProvider, _mutexTimeoutMilliseconds ) );
    }

    private ITelemetryService TelemetryService => this.ServiceProvider.GetRequiredBackstageService<ITelemetryService>();

    /// <summary>
    /// Creates the <c>.git</c> directory that gives the project a repository context, without which telemetry is
    /// disabled outright and no configuration is read at all.
    /// </summary>
    private void CreateRepository() => this.FileSystem.CreateDirectory( Path.Combine( _repositoryDirectory, ".git" ) );

    private ITelemetryContext OpenTelemetryContext() => this.TelemetryService.OpenContext( this.TelemetryService.GetPolicy( _projectDirectory ) );

    [Fact]
    public void UsageSessionIsStartedWhenTheMutexIsUnavailable()
    {
        // The signature reported most often: the mutex is unavailable while the compiler constructs its services and
        // starts its usage session.
        this.CreateRepository();
        this.EnsureServicesInitialized();

        using ( new HeldConfigurationMutex( this.ServiceProvider ) )
        {
            var context = this.OpenTelemetryContext();

            using ( context.StartUsageSession( "TestUsage", "Project1" ) ) { }
        }
    }

    [Fact]
    public void UsageSessionIsDisposedWhenTheMutexIsUnavailable()
    {
        // The mutex becomes unavailable only at the end of the compilation, when the usage session is disposed. The
        // compilation has already succeeded at that point, so failing it is never acceptable.
        this.CreateRepository();

        var context = this.OpenTelemetryContext();
        var session = context.StartUsageSession( "TestUsage", "Project1" );
        Assert.True( session.ShouldCollectMetrics );

        using ( new HeldConfigurationMutex( this.ServiceProvider ) )
        {
            session.Dispose();
        }
    }

    [Fact]
    public void TelemetryUploadIsScheduledWhenTheMutexIsUnavailable()
    {
        // Scheduling the upload of a telemetry report reads and writes the configuration, to claim the daily upload
        // budget. When the mutex is unavailable, no upload is scheduled, and that is all that happens.
        this.CreateRepository();
        this.TelemetryConfigurationService.SetConsent( TelemetryConsent.Yes );
        this.TelemetryConfigurationService.EnsureActivated();

        // The uploader does not upload anything during the first fifteen minutes of the lifetime of the device.
        this.Time.AddTime( TimeSpan.FromMinutes( 20 ) );

        var uploader = this.ServiceProvider.GetRequiredBackstageService<ITelemetryUploader>();

        using ( new HeldConfigurationMutex( this.ServiceProvider ) )
        {
            Assert.False( uploader.StartUpload() );
        }

        Assert.Empty( this.ProcessExecutor.StartedProcesses );
    }
}
