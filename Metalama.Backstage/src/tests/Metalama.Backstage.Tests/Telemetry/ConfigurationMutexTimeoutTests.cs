// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Telemetry;

/// <summary>
/// Verifies that a failure to acquire the global configuration mutex never aborts the operation that happens to be
/// running when it occurs.
/// </summary>
/// <remarks>
/// <para>
/// See #1847. <c>ConfigurationManager.WithMutex</c> waits at most thirty seconds for the cross-process
/// <c>Global\Metalama.Configuration</c> mutex and then throws a <see cref="TimeoutException"/>. Every reported crash
/// reached that throw on behalf of telemetry, and the exception propagated out of the compiler or of the design-time
/// analyzer, which aborted the compilation. Telemetry is a best-effort background activity, so losing a usage metric
/// or an upload must degrade silently instead.
/// </para>
/// <para>
/// The timeout is simulated rather than provoked: an <see cref="IConfigurationManager"/> decorator throws the very
/// exception that <c>WithMutex</c> throws. That keeps the test deterministic and instantaneous, whereas holding the
/// real mutex would make every test wait for the real timeout.
/// </para>
/// </remarks>
public sealed class ConfigurationMutexTimeoutTests : TestsBase
{
    private const string _repositoryDirectory = @"C:\repo";
    private const string _projectDirectory = @"C:\repo\src\project";

    public ConfigurationMutexTimeoutTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo { IsTelemetryEnabled = true } ) { }

    protected override void ConfigureServices( ServiceProviderBuilder services )
    {
        services.AddTelemetryServices();

        services.AddSingleton<IConfigurationManager>(
            serviceProvider => new UnavailableMutexConfigurationManager( new InMemoryConfigurationManager( serviceProvider ) ) );
    }

    /// <summary>
    /// Gets the configuration manager decorator, whose <see cref="UnavailableMutexConfigurationManager.IsMutexUnavailable"/>
    /// property simulates the mutex timeout.
    /// </summary>
    private UnavailableMutexConfigurationManager ConfigurationManagerDecorator
        => (UnavailableMutexConfigurationManager) this.ServiceProvider.GetRequiredBackstageService<IConfigurationManager>();

    private ITelemetryService TelemetryService => this.ServiceProvider.GetRequiredBackstageService<ITelemetryService>();

    /// <summary>
    /// Creates the <c>.git</c> directory that gives the project a repository context, without which telemetry is
    /// disabled outright and none of the configuration would be read.
    /// </summary>
    private void CreateRepository() => this.FileSystem.CreateDirectory( Path.Combine( _repositoryDirectory, ".git" ) );

    [Fact]
    public void UsageSessionIsStartedWhenTheConfigurationMutexIsUnavailable()
    {
        // The signature reported most often: the mutex is unavailable while the compiler is constructing its services,
        // and the usage session decides whether it should collect metrics.
        this.CreateRepository();
        this.EnsureServicesInitialized();

        this.ConfigurationManagerDecorator.IsMutexUnavailable = true;

        var context = this.TelemetryService.OpenContext( this.TelemetryService.GetPolicy( _projectDirectory ) );

        using ( var session = context.StartUsageSession( "TestUsage", "Project1" ) )
        {
            // Metrics cannot be collected, because whether this session is a duplicate cannot be determined without the
            // configuration. That is the acceptable degradation: the build itself must go on.
            Assert.False( session.ShouldCollectMetrics );
        }
    }

    [Fact]
    public void UsageSessionIsDisposedWhenTheConfigurationMutexIsUnavailable()
    {
        // The mutex becomes unavailable only at the end of the compilation, when the usage session is disposed. The
        // compilation has already succeeded at that point, so aborting it is never acceptable.
        this.CreateRepository();
        this.EnsureServicesInitialized();

        var context = this.TelemetryService.OpenContext( this.TelemetryService.GetPolicy( _projectDirectory ) );
        var session = context.StartUsageSession( "TestUsage", "Project1" );
        Assert.True( session.ShouldCollectMetrics );

        this.ConfigurationManagerDecorator.IsMutexUnavailable = true;

        session.Dispose();
    }

    [Fact]
    public void TelemetryUploadIsStartedWhenTheConfigurationMutexIsUnavailable()
    {
        // Scheduling the upload of a telemetry report reads the configuration to decide whether the daily budget has
        // been consumed. When it cannot, no upload is scheduled, and that is all that happens.
        this.CreateRepository();
        this.TelemetryConfigurationService.SetConsent( TelemetryConsent.Yes );
        this.TelemetryConfigurationService.EnsureActivated();

        // The uploader does not upload anything during the first fifteen minutes of the lifetime of the device.
        this.Time.AddTime( TimeSpan.FromMinutes( 20 ) );

        this.ConfigurationManagerDecorator.IsMutexUnavailable = true;

        var uploader = this.ServiceProvider.GetRequiredBackstageService<ITelemetryUploader>();

        Assert.False( uploader.StartUpload() );
        Assert.Empty( this.ProcessExecutor.StartedProcesses );
    }

    /// <summary>
    /// An <see cref="IConfigurationManager"/> decorator that throws the exception thrown by
    /// <c>ConfigurationManager.WithMutex</c> when the global configuration mutex cannot be acquired, so that the
    /// exception handling of the callers can be exercised without waiting for the real timeout.
    /// </summary>
    private sealed class UnavailableMutexConfigurationManager : IConfigurationManager
    {
        private readonly IConfigurationManager _underlying;

        public UnavailableMutexConfigurationManager( IConfigurationManager underlying )
        {
            this._underlying = underlying;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the global configuration mutex is unavailable, in which case every
        /// operation that requires it throws.
        /// </summary>
        public bool IsMutexUnavailable { get; set; }

        public ILogger Logger => this._underlying.Logger;

        public event Action<ConfigurationFile> ConfigurationFileChanged
        {
            add => this._underlying.ConfigurationFileChanged += value;
            remove => this._underlying.ConfigurationFileChanged -= value;
        }

        public string GetFilePath( string fileName ) => this._underlying.GetFilePath( fileName );

        public string GetFilePath( Type type ) => this._underlying.GetFilePath( type );

        public ConfigurationFile Get( Type type, bool ignoreCache = false )
        {
            this.ThrowIfMutexUnavailable();

            return this._underlying.Get( type, ignoreCache );
        }

        public bool TryUpdate( ConfigurationFile value, ConfigurationFileTimestamp? expectedTimestamp )
        {
            this.ThrowIfMutexUnavailable();

            return this._underlying.TryUpdate( value, expectedTimestamp );
        }

        public void Dispose() => this._underlying.Dispose();

        private void ThrowIfMutexUnavailable()
        {
            if ( this.IsMutexUnavailable )
            {
                throw new TimeoutException( "Cannot acquire the global configuration mutex in 30s." );
            }
        }
    }
}
