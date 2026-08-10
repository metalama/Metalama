// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Serialization;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Threading;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.ConfigurationManager;

/// <summary>
/// Stresses <see cref="Configuration.ConfigurationManager"/> against the real file system, the real named locks and
/// the real synchronization objects of the operating system.
/// </summary>
/// <remarks>
/// <para>
/// These tests are excluded from continuous integration: they run for as long as they are given and their purpose
/// is to look for a defect, not to assert a property that a deterministic test could assert. Run them manually
/// after any change to the locking, once with the processors saturated and once without, on both target
/// frameworks. Any failure must be turned into a deterministic test before it is considered fixed.
/// </para>
/// <para>
/// Each worker has its own <see cref="Configuration.ConfigurationManager"/> over one directory, which is what a
/// machine running several Metalama processes looks like. The directory is created afresh for each run: the load
/// test this one replaces wrote to the real application data directory of the developer, so it raced whatever
/// Metalama process happened to be running on the machine and could not be run on two target frameworks at once.
/// </para>
/// </remarks>
public sealed class ConfigurationManagerStressTests : IDisposable
{
    /// <summary>
    /// The number of concurrent writers, each with a configuration manager of its own.
    /// </summary>
    private const int _workerCount = 8;

    /// <summary>
    /// The number of updates each writer attempts.
    /// </summary>
    /// <remarks>
    /// Eight writers and a thousand updates each take roughly two minutes with the processors saturated, which
    /// leaves room under the timeout. Raise both freely when running the test by hand: the assertions do not depend
    /// on either value.
    /// </remarks>
    private const int _iterationsPerWorker = 1000;

    private readonly ITestOutputHelper _logger;
    private readonly string _directory;

    public ConfigurationManagerStressTests( ITestOutputHelper logger )
    {
        this._logger = logger;

        this._directory = Path.Combine( Path.GetTempPath(), "Metalama.ConfigurationManagerStressTests", Guid.NewGuid().ToString( "N" ) );
        Directory.CreateDirectory( this._directory );
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete( this._directory, recursive: true );
        }
        catch ( Exception e )
        {
            this._logger.WriteLine( $"Could not delete '{this._directory}': {e.Message}" );
        }
    }

    /// <summary>
    /// Runs the stress without competing for the processors.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// Load test: excluded from continuous integration. See the remarks of the class.
    /// </remarks>
    [Fact( Timeout = 600000, Skip = "Load test - run manually (see remarks)." )]
    public Task ConcurrentUpdatesNeverLoseAnUpdate() => this.RunStressAsync( withCpuLoad: false );

    /// <summary>
    /// Runs the stress while the processors are saturated, so that the threads of the code under test are
    /// preempted inside their critical sections instead of running them to completion.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// Load test: excluded from continuous integration. See the remarks of the class.
    /// </remarks>
    [Fact( Timeout = 600000, Skip = "Load test - run manually (see remarks)." )]
    public Task ConcurrentUpdatesNeverLoseAnUpdateUnderCpuLoad() => this.RunStressAsync( withCpuLoad: true );

    /// <summary>
    /// Builds a service provider over the real file system and the real named locks, rooted at the directory of
    /// this test.
    /// </summary>
    /// <returns>The service provider.</returns>
    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IFileSystem>( new FileSystem() );
        services.AddSingleton<IDateTimeProvider>( new TestDateTimeProvider() );
        services.AddSingleton<IEnvironmentVariableProvider>( new EnvironmentVariableProvider() );
        services.AddSingleton<IRuntimeInformation>( new RuntimeInformationProvider() );
        services.AddSingleton<EarlyLoggerFactory>();
        services.AddSingleton<IStandardDirectories>( new StressDirectories( this._directory ) );

        // The real service and not the substitute, because this test uses the real file system and therefore has to
        // exclude the other processes of the machine exactly as the product does.
        services.AddSingleton<INamedLockService>( new NamedLockService() );

        services.AddSingleton<IJsonSerializationService>(
            _ => new JsonSerializationService( new IJsonTypeInfoResolver[] { TestConfigurationJsonContext.Default } ) );

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Runs the workers and verifies the result.
    /// </summary>
    /// <param name="withCpuLoad">Whether to saturate the processors while the workers run.</param>
    /// <returns>A task that completes when the test does.</returns>
    private async Task RunStressAsync( bool withCpuLoad )
    {
        var serviceProvider = this.CreateServiceProvider();

        using ( var cpuLoad = withCpuLoad ? new CpuLoadGenerator( this._logger.WriteLine ) : null )
        {
            _ = cpuLoad;

            var outcomes = new ConcurrentQueue<ConfigurationUpdateOutcome>();
            var acceptedByWorker = new List<string>[_workerCount];

            var workers = new Task[_workerCount];

            for ( var workerId = 0; workerId < _workerCount; workerId++ )
            {
                var capturedWorkerId = workerId;
                acceptedByWorker[workerId] = new List<string>();

                workers[workerId] = Task.Factory.StartNew(
                    () => RunWorker( serviceProvider, capturedWorkerId, acceptedByWorker[capturedWorkerId], outcomes ),
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default );
            }

            await Task.WhenAll( workers );

            this.Verify( serviceProvider, acceptedByWorker, outcomes );
        }
    }

    /// <summary>
    /// Appends one mark per iteration, through a manager of its own, and records which of its attempts were
    /// accepted.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="workerId">The identifier of this worker.</param>
    /// <param name="accepted">The list to which the marks of the accepted attempts are added.</param>
    /// <param name="outcomes">The queue to which every outcome is added, for the report.</param>
    private static void RunWorker(
        IServiceProvider serviceProvider,
        int workerId,
        List<string> accepted,
        ConcurrentQueue<ConfigurationUpdateOutcome> outcomes )
    {
        // One manager per worker: two managers over one directory are two processes as far as the locks are
        // concerned, which is the situation this test exists to exercise.
        using var configurationManager = new Configuration.ConfigurationManager( serviceProvider );

        for ( var iteration = 0; iteration < _iterationsPerWorker; iteration++ )
        {
            var mark = FormatMark( workerId, iteration );

            var outcome = configurationManager.Update(
                typeof(TestConfigurationFile),
                currentValue => ((TestConfigurationFile) currentValue) with { Marks = ((TestConfigurationFile) currentValue).Marks + mark } );

            outcomes.Enqueue( outcome );

            if ( outcome == ConfigurationUpdateOutcome.Updated )
            {
                accepted.Add( mark );
            }
        }
    }

    /// <summary>
    /// Composes the mark that one attempt of one worker appends.
    /// </summary>
    /// <param name="workerId">The identifier of the worker.</param>
    /// <param name="iteration">The number of the attempt within the worker.</param>
    /// <returns>The mark.</returns>
    private static string FormatMark( int workerId, int iteration )
        => string.Format( CultureInfo.InvariantCulture, "w{0}:{1};", workerId, iteration );

    /// <summary>
    /// Verifies that the file holds exactly the marks of the accepted attempts, once each.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="acceptedByWorker">The marks each worker was told had been written.</param>
    /// <param name="outcomes">Every outcome, for the report.</param>
    /// <remarks>
    /// Counting the accepted attempts cannot detect a lost update, because a lost update leaves the count right and
    /// the content wrong. What detects one is that every mark an accepted attempt was told had been written is
    /// still in the file at the end.
    /// </remarks>
    private void Verify(
        IServiceProvider serviceProvider,
        IReadOnlyList<List<string>> acceptedByWorker,
        ConcurrentQueue<ConfigurationUpdateOutcome> outcomes )
    {
        using var reader = new Configuration.ConfigurationManager( serviceProvider );
        var finalValue = reader.Get<TestConfigurationFile>( true );

        var marksInFile = finalValue.Marks.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries );

        this._logger.WriteLine( this.FormatReport( acceptedByWorker, outcomes, marksInFile.Length, finalValue.Version ) );

        var marksInFileSet = new HashSet<string>( marksInFile.Select( m => m + ";" ), StringComparer.Ordinal );

        // No mark appears twice, which would mean that a transformation ran against a value that was written twice.
        Assert.Equal( marksInFileSet.Count, marksInFile.Length );

        var totalAccepted = 0;

        for ( var workerId = 0; workerId < acceptedByWorker.Count; workerId++ )
        {
            var accepted = acceptedByWorker[workerId];
            totalAccepted += accepted.Count;

            foreach ( var mark in accepted )
            {
                Assert.True( marksInFileSet.Contains( mark ), $"The update '{mark}' was accepted but is not in the file: it was lost." );
            }
        }

        // Every mark in the file was accepted by somebody, so nothing appeared out of nowhere.
        Assert.Equal( totalAccepted, marksInFile.Length );

        // One write per accepted update, and no write that no accepted update accounts for.
        Assert.Equal( totalAccepted, finalValue.Version );
    }

    /// <summary>
    /// Composes the report written to the test output, so that a failure can be understood without running the
    /// test again.
    /// </summary>
    /// <param name="acceptedByWorker">The marks each worker was told had been written.</param>
    /// <param name="outcomes">Every outcome.</param>
    /// <param name="marksInFile">The number of marks found in the file.</param>
    /// <param name="version">The version the file ended at.</param>
    /// <returns>The report.</returns>
    private string FormatReport(
        IReadOnlyList<List<string>> acceptedByWorker,
        ConcurrentQueue<ConfigurationUpdateOutcome> outcomes,
        int marksInFile,
        int? version )
    {
        var report = new StringBuilder();

        report.AppendLine( FormattableString.Invariant( $"Directory: {this._directory}" ) );
        report.AppendLine( FormattableString.Invariant( $"Workers: {_workerCount}, iterations each: {_iterationsPerWorker}" ) );

        foreach ( var outcomeGroup in outcomes.GroupBy( o => o ).OrderBy( g => g.Key.ToString(), StringComparer.Ordinal ) )
        {
            report.AppendLine( FormattableString.Invariant( $"  {outcomeGroup.Key}: {outcomeGroup.Count()}" ) );
        }

        for ( var workerId = 0; workerId < acceptedByWorker.Count; workerId++ )
        {
            report.AppendLine( FormattableString.Invariant( $"  worker {workerId}: {acceptedByWorker[workerId].Count} accepted" ) );
        }

        report.AppendLine( FormattableString.Invariant( $"Marks in the file: {marksInFile}, version: {version}" ) );

        return report.ToString();
    }

    /// <summary>
    /// Roots every standard directory at the directory of one run of this test.
    /// </summary>
    private sealed class StressDirectories : IStandardDirectories
    {
        public StressDirectories( string root )
        {
            this.ApplicationDataDirectory = root;
            this.TempDirectory = Path.Combine( root, "Temp" );
            this.TelemetryDirectory = Path.Combine( root, "Telemetry" );
        }

        public string ApplicationDataDirectory { get; }

        public string TempDirectory { get; }

        public string TelemetryDirectory { get; }

        public IReadOnlyList<string> LegacyTempDirectories => Array.Empty<string>();

        public string TelemetryLogsDirectory => Path.Combine( this.TelemetryDirectory, "Logs" );

        public string TelemetryExceptionsDirectory => Path.Combine( this.TelemetryDirectory, "Exceptions" );

        public string TelemetryUploadQueueDirectory => Path.Combine( this.TelemetryDirectory, "UploadQueue" );

        public string TelemetryUploadPackagesDirectory => Path.Combine( this.TelemetryDirectory, "UploadPackages" );

        public string CrashReportsDirectory => Path.Combine( this.TelemetryDirectory, "CrashReports" );
    }
}
