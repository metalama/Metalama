// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Serialization;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Threading;
using Metalama.Testing.Hooks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.ConfigurationManager;

/// <summary>
/// Tests how <see cref="Configuration.ConfigurationManager"/> uses its locks: which operations take one, which take
/// none, and what happens when one cannot be acquired.
/// </summary>
/// <remarks>
/// The locks are the substitute <see cref="TestNamedLockService"/>, which uses no operating system object, records
/// every acquisition and fails the test rather than hanging when the code under test breaks the locking discipline.
/// Every wait is released by another thread through a synchronization point, so no assertion depends on a duration.
/// </remarks>
public sealed class ConfigurationManagerLockingTests : TestsBase, IDisposable
{
    /// <summary>
    /// Bounds every wait in this class, so that a defect surfaces as a failed assertion rather than as a hung test
    /// run. It is a guard and never a synchronization mechanism.
    /// </summary>
    private readonly CancellationTokenSource _timeout = new( TimeSpan.FromSeconds( 30 ) );

    private readonly TestSynchronizationProvider _syncProvider;

    public ConfigurationManagerLockingTests( ITestOutputHelper logger ) : base(
        logger,
        applicationInfo: new TestApplicationInfo() { IsLongRunningProcess = true } )
    {
        this._syncProvider = new TestSynchronizationProvider( logger.WriteLine );

        this.InitializationOptions = this.InitializationOptions with
        {
            AdditionalJsonTypeInfoResolvers = new IJsonTypeInfoResolver[] { TestConfigurationJsonContext.Default }
        };
    }

    /// <inheritdoc />
    protected override void ConfigureServices( ServiceProviderBuilder services )
    {
        // Registered untyped, because ITestSynchronizationProvider is shared with the layers above
        // Metalama.Backstage and therefore derives from no dependency injection marker interface.
        services.AddService( typeof(ITestSynchronizationProvider), this._syncProvider );
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Releasing every point first guarantees that no thread is left pinned inside the code under test while
        // holding a lock.
        this._syncProvider.Dispose();
        this._timeout.Dispose();
    }

    private Configuration.ConfigurationManager CreateConfigurationManager() => new( this.ServiceProvider );

    /// <summary>
    /// Gets the name of the lock that protects a configuration file.
    /// </summary>
    /// <typeparam name="T">The type of the configuration file.</typeparam>
    /// <param name="configurationManager">The manager.</param>
    /// <returns>The name of the lock.</returns>
    private static string GetLockName<T>( IConfigurationManager configurationManager )
        where T : ConfigurationFile
        => NamedLockExtensions.GetGlobalLockName( configurationManager.GetFilePath<T>() );

    /// <summary>
    /// Runs an action on a thread of its own, so that a test can drive it while it is blocked.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns>A task that completes when the action returns.</returns>
    /// <remarks>
    /// No cancellation token is passed to the scheduler on purpose: a token that is already signalled makes the
    /// delegate never run, which would leave the signals awaited by the caller unset.
    /// </remarks>
    private static Task RunOnDedicatedThreadAsync( Action action )
        => Task.Factory.StartNew( action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default );

    /// <summary>
    /// Writes a configuration file directly, as another process that does not go through the manager would.
    /// </summary>
    /// <typeparam name="T">The type of the configuration file.</typeparam>
    /// <param name="configurationManager">The manager, used only to locate the file.</param>
    /// <param name="value">The value to write, whose <see cref="ConfigurationFile.Version"/> must be set explicitly.</param>
    private void WriteFileExternally<T>( IConfigurationManager configurationManager, T value )
        where T : ConfigurationFile
    {
        var json = this.ServiceProvider.GetRequiredBackstageService<IJsonSerializationService>().Serialize( value, typeof(T) );
        this.FileSystem.WriteAllText( configurationManager.GetFilePath<T>(), json );
    }

    /// <summary>
    /// Awaits a task, failing rather than hanging if <see cref="_timeout"/> elapses first.
    /// </summary>
    /// <param name="task">The task to await.</param>
    /// <returns>A task that completes when <paramref name="task"/> does.</returns>
    private async Task WithTimeout( Task task )
    {
        var timedOut = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        using ( this._timeout.Token.Register( () => timedOut.TrySetResult( true ) ) )
        {
            if ( await Task.WhenAny( task, timedOut.Task ) != task )
            {
                throw new TimeoutException( "The test timed out while waiting for the code under test." );
            }
        }

        await task;
    }

    /// <summary>
    /// Verifies that reading a configuration file takes no lock whatsoever, which is what keeps a read off the
    /// critical path of every other operation.
    /// </summary>
    [Fact]
    public void ReadingTakesNoLock()
    {
        using var configurationManager = this.CreateConfigurationManager();

        // Once from the file, which does not exist, and once from the cache.
        Assert.Null( configurationManager.Get<TestConfigurationFile>().Timestamp );
        Assert.Null( configurationManager.Get<TestConfigurationFile>( true ).Timestamp );

        Assert.Empty( this.Locks.GetKnownNames() );
    }

    /// <summary>
    /// Verifies that a read of a file that has been written takes no lock either, so that the absence of a lock is
    /// not an artefact of the file being missing.
    /// </summary>
    [Fact]
    public void ReadingAWrittenFileTakesNoLock()
    {
        using var configurationManager = this.CreateConfigurationManager();
        var lockName = GetLockName<TestConfigurationFile>( configurationManager );

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );

        var acquisitionsAfterUpdate = this.Locks.GetAcquisitionCount( lockName );

        Assert.True( configurationManager.Get<TestConfigurationFile>().IsModified );
        Assert.True( configurationManager.Get<TestConfigurationFile>( true ).IsModified );

        Assert.Equal( acquisitionsAfterUpdate, this.Locks.GetAcquisitionCount( lockName ) );
    }

    /// <summary>
    /// Gets the name of the lock that the versions of Metalama preceding this class take, which is derived from
    /// the data directory rather than from the path of a file.
    /// </summary>
    /// <returns>The name of the lock.</returns>
    private string GetLegacyLockName()
        => NamedLockExtensions.GetGlobalLockName(
            this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>().ApplicationDataDirectory );

    /// <summary>
    /// Verifies that an update does not take the lock of the previous generation unless it is asked to.
    /// </summary>
    /// <remarks>
    /// The default matters: taking that lock serializes every write of every configuration file against every
    /// other, which is the behaviour this class exists to remove.
    /// </remarks>
    [Fact]
    public void TheLegacyLockIsNotTakenByDefault()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );

        Assert.Equal( new[] { GetLockName<TestConfigurationFile>( configurationManager ) }, this.Locks.GetKnownNames() );
        Assert.Equal( 0, this.Locks.GetAcquisitionCount( this.GetLegacyLockName() ) );
    }

    /// <summary>
    /// Verifies that the environment variable makes an update additionally take the lock of the previous
    /// generation, under the name that generation uses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The name is the whole point: mutual exclusion with a process of another generation is obtained only if both
    /// name the same operating system object, and that name is derived from the data directory. Asserting on the
    /// composed name rather than merely on the count of acquisitions is what makes this a compatibility test.
    /// </para>
    /// <para>
    /// The locking discipline of the substitute is relaxed for this test, because holding two named locks at once
    /// is exactly what is being verified. The nesting is safe here because the order is fixed, the lock of the
    /// previous generation always being taken first, and because no other operation of this class acquires either
    /// of the two.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheEnvironmentVariableMakesAnUpdateTakeTheLegacyLock()
    {
        this.Locks.EnforceDiscipline = false;
        this.EnvironmentVariableProvider.Environment[Configuration.ConfigurationManager.LegacyLockEnvironmentVariableName] = "true";

        using var configurationManager = this.CreateConfigurationManager();

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );

        var legacyLockName = this.GetLegacyLockName();

        Assert.Equal( 1, this.Locks.GetAcquisitionCount( legacyLockName ) );
        Assert.Equal( 1, this.Locks.GetAcquisitionCount( GetLockName<TestConfigurationFile>( configurationManager ) ) );

        // Both were released, and the update took effect.
        Assert.Empty( this.Locks.GetHeldLocks() );
        Assert.True( configurationManager.Get<TestConfigurationFile>( true ).IsModified );

        // The nesting was recorded rather than thrown, which is what relaxing the discipline means.
        Assert.NotEmpty( this.Locks.Violations );
    }

    /// <summary>
    /// Verifies that a value which does not express assent leaves the lock of the previous generation alone.
    /// </summary>
    /// <param name="value">The value of the environment variable.</param>
    /// <remarks>
    /// The variable named after a feature and set to <c>false</c> must switch that feature off, which is not what
    /// treating the mere presence of the variable as assent would do.
    /// </remarks>
    [Theory]
    [InlineData( "false" )]
    [InlineData( "0" )]
    [InlineData( "" )]
    [InlineData( "yes" )]
    public void AValueThatDoesNotExpressAssentLeavesTheLegacyLockAlone( string value )
    {
        this.EnvironmentVariableProvider.Environment[Configuration.ConfigurationManager.LegacyLockEnvironmentVariableName] = value;

        using var configurationManager = this.CreateConfigurationManager();

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );

        Assert.Equal( 0, this.Locks.GetAcquisitionCount( this.GetLegacyLockName() ) );
    }

    /// <summary>
    /// Verifies that an update is declined, and takes no further lock, when the lock of the previous generation is
    /// held by another process.
    /// </summary>
    /// <remarks>
    /// This is the exclusion the variable exists to obtain, seen from this side: a process of the previous
    /// generation holding its directory-wide lock now keeps this one out.
    /// </remarks>
    [Fact]
    public void AnUpdateWaitsForTheLegacyLockWhenItIsHeldElsewhere()
    {
        this.Locks.EnforceDiscipline = false;
        this.EnvironmentVariableProvider.Environment[Configuration.ConfigurationManager.LegacyLockEnvironmentVariableName] = "true";

        using var configurationManager = this.CreateConfigurationManager();

        this.Locks.ForceTimeout( this.GetLegacyLockName(), int.MaxValue );

        Assert.Equal(
            ConfigurationUpdateOutcome.LockTimeout,
            configurationManager.Update(
                typeof(TestConfigurationFile),
                currentValue => ((TestConfigurationFile) currentValue) with { IsModified = true } ) );

        // The per-file lock was never reached, so nothing was written.
        Assert.Equal( 0, this.Locks.GetAcquisitionCount( GetLockName<TestConfigurationFile>( configurationManager ) ) );
        Assert.Null( configurationManager.Get<TestConfigurationFile>( true ).Timestamp );
    }

    /// <summary>
    /// Verifies that the lock protecting a configuration file is named after the path of the file.
    /// </summary>
    /// <remarks>
    /// The type cannot be the identity of the lock: the same file is represented by a different type in each copy
    /// of the declaring assembly loaded in the process, and two distinct types can be stored in the same file.
    /// </remarks>
    [Fact]
    public void TheLockIsNamedAfterTheFilePath()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );

        Assert.Equal( new[] { GetLockName<TestConfigurationFile>( configurationManager ) }, this.Locks.GetKnownNames() );
    }

    /// <summary>
    /// Verifies that updating one configuration file does not wait for an unrelated file whose lock is held by
    /// another process.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// This is the granularity regression. The implementation this one replaces protected the whole directory with
    /// a single lock, so an update of any file waited for an update of every other.
    /// </remarks>
    [Fact]
    public async Task UpdatesOfDifferentFilesDoNotWaitForEachOther()
    {
        using var configurationManager = this.CreateConfigurationManager();

        using ( this.Locks.Pin( GetLockName<TestConfigurationFile>( configurationManager ) ) )
        {
            // The lock of the first file is held by another process for the whole scope, and the second file is
            // nonetheless updated.
            await this.WithTimeout(
                RunOnDedicatedThreadAsync(
                    () => Assert.True( configurationManager.Update<SecondTestConfigurationFile>( c => c with { IsModified = true } ) ) ) );
        }

        Assert.True( configurationManager.Get<SecondTestConfigurationFile>().IsModified );
    }

    /// <summary>
    /// Verifies that a handler of <see cref="IConfigurationManager.ConfigurationFileChanged"/> holds no lock, and
    /// can therefore update another configuration file without deadlocking.
    /// </summary>
    [Fact]
    public void AHandlerHoldsNoLockAndCanUpdateAnotherFile()
    {
        using var configurationManager = this.CreateConfigurationManager();

        // Establishes the cached value, so that the update below is a change rather than a first read.
        Assert.False( configurationManager.Get<TestConfigurationFile>().IsModified );

        var locksHeldInHandler = new ConcurrentQueue<IReadOnlyList<string>>();

        configurationManager.ConfigurationFileChanged += _ =>
        {
            locksHeldInHandler.Enqueue( this.Locks.GetLocksHeldByCurrentThread() );
            configurationManager.Update<SecondTestConfigurationFile>( c => c with { IsModified = true } );
        };

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );

        // The handler runs for the first file and again for the second one that it updated itself, and holds no
        // lock either time.
        Assert.NotEmpty( locksHeldInHandler );
        Assert.All( locksHeldInHandler, Assert.Empty );
        Assert.True( configurationManager.Get<SecondTestConfigurationFile>().IsModified );
    }

    /// <summary>
    /// Verifies that a handler that updates the very file it is being notified about terminates instead of
    /// deadlocking or recurring indefinitely.
    /// </summary>
    /// <remarks>
    /// Dispatching outside the lock removes the deadlock but opens the possibility of a cascade, so the handler
    /// must reach a state in which it has nothing more to change. This one does, because its update is a no-op once
    /// the value is in the desired state.
    /// </remarks>
    [Fact]
    public void AHandlerUpdatingTheSameFileTerminates()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.False( configurationManager.Get<TestConfigurationFile>().IsModified );

        var invocations = 0;

        configurationManager.ConfigurationFileChanged += _ =>
        {
            Interlocked.Increment( ref invocations );
            configurationManager.UpdateIf<TestConfigurationFile>( c => !c.IsModified, c => c with { IsModified = true } );
        };

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );

        Assert.Equal( 1, invocations );
    }

    /// <summary>
    /// Verifies that a handler that throws neither fails the update nor deprives the handlers registered after it
    /// of the notification.
    /// </summary>
    /// <remarks>
    /// A multicast invocation stops at the first handler that throws. The implementation this one replaces invoked
    /// the delegate that way, so one faulty subscriber silently suppressed every subsequent one.
    /// </remarks>
    [Fact]
    public void AThrowingHandlerDoesNotPreventTheFollowingOnes()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.False( configurationManager.Get<TestConfigurationFile>().IsModified );

        var secondHandlerInvocations = 0;

        configurationManager.ConfigurationFileChanged += _ => throw new InvalidOperationException( "Injected by a test." );
        configurationManager.ConfigurationFileChanged += _ => Interlocked.Increment( ref secondHandlerInvocations );

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );

        Assert.Equal( 1, secondHandlerInvocations );
        Assert.Contains( this.Log.Entries, e => e.Severity == TestLoggerFactory.Severity.Error );
    }

    /// <summary>
    /// Verifies that an update completes when the clock does not advance.
    /// </summary>
    /// <remarks>
    /// The implementation this one replaces waited, while holding the lock, for the clock to return a value
    /// different from the modification time of the file it had just read, which never happens under a stopped
    /// clock. The version carried by <see cref="ConfigurationFileTimestamp"/> already distinguishes two writes that
    /// share a modification time, so no wait is needed.
    /// </remarks>
    [Fact]
    public void UpdatesCompleteWhenTheClockDoesNotAdvance()
    {
        this.Time.Stop();

        using var configurationManager = this.CreateConfigurationManager();

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );
        Assert.True( configurationManager.UpdateIf<TestConfigurationFile>( c => c.IsModified, c => c with { IsModified = false } ) );

        var value = configurationManager.Get<TestConfigurationFile>( true );
        Assert.False( value.IsModified );
        Assert.Equal( 2, value.Version );
    }

    /// <summary>
    /// Verifies that an update whose lock cannot be acquired is declined rather than raising an exception, and that
    /// it is reported once.
    /// </summary>
    /// <remarks>
    /// Failing to write a configuration file must never fail a compilation. The implementation this one replaces
    /// threw a <see cref="TimeoutException"/> here, which is issue 1847 as it was reported.
    /// </remarks>
    [Fact]
    public void AnUpdateIsDeclinedWhenTheLockCannotBeAcquired()
    {
        this.Time.Stop();

        using var configurationManager = this.CreateConfigurationManager();

        this.Locks.ForceTimeout( GetLockName<TestConfigurationFile>( configurationManager ), int.MaxValue );

        Assert.Equal( ConfigurationUpdateOutcome.LockTimeout, UpdateTestFile( configurationManager ) );
        Assert.Equal( ConfigurationUpdateOutcome.LockTimeout, UpdateTestFile( configurationManager ) );

        // Reported once and not twice: on a machine on which the condition holds, it holds for every operation.
        Assert.Single(
            this.Log.Entries,
            e => e.Severity == TestLoggerFactory.Severity.Warning && e.Message.Contains( "Timeout while waiting", StringComparison.Ordinal ) );

        Assert.DoesNotContain( this.Log.Entries, e => e.Severity == TestLoggerFactory.Severity.Error );
    }

    /// <summary>
    /// Verifies that an exception raised by the lock service does not escape an update.
    /// </summary>
    [Fact]
    public void AnExceptionOfTheLockServiceDoesNotEscape()
    {
        using var configurationManager = this.CreateConfigurationManager();

        this.Locks.ArmException(
            GetLockName<TestConfigurationFile>( configurationManager ),
            () => new UnauthorizedAccessException( "Injected by a test." ) );

        Assert.Equal( ConfigurationUpdateOutcome.LockTimeout, UpdateTestFile( configurationManager ) );
    }

    /// <summary>
    /// Sets <see cref="TestConfigurationFile.IsModified"/>, and reports what happened.
    /// </summary>
    /// <param name="configurationManager">The manager.</param>
    /// <returns>The outcome.</returns>
    private static ConfigurationUpdateOutcome UpdateTestFile( IConfigurationManager configurationManager )
        => configurationManager.Update( typeof(TestConfigurationFile), current => ((TestConfigurationFile) current) with { IsModified = true } );

    /// <summary>
    /// Verifies that reading a file that has gone backwards on disk does not make the cache go backwards with it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A read takes no lock, so two readers of two versions of the same file can store their results in either
    /// order. What keeps the cache correct is that it only ever moves forward, and this is that rule in its
    /// simplest observable form.
    /// </para>
    /// <para>
    /// The clock is stopped so that both versions carry the same modification time, which leaves the version
    /// number as the only thing distinguishing them. That is the case in which the comparison is easiest to get
    /// wrong, and it is also the ordinary case during a build, where several updates land within one tick.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheCacheDoesNotAdoptAnOlderValue()
    {
        this.Time.Stop();

        using var configurationManager = this.CreateConfigurationManager();

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );
        Assert.True( configurationManager.UpdateIf<TestConfigurationFile>( c => c.IsModified, c => c with { IsModified = false } ) );
        Assert.Equal( 2, configurationManager.Get<TestConfigurationFile>().Version );

        // The file goes back to what version 1 held, with the same modification time.
        this.WriteFileExternally( configurationManager, new TestConfigurationFile { IsModified = true, Version = 1 } );

        // The caller that asked for the file receives what the file holds.
        var valueFromFile = configurationManager.Get<TestConfigurationFile>( true );
        Assert.True( valueFromFile.IsModified );
        Assert.Equal( 1, valueFromFile.Version );

        // The cache keeps the version it had.
        var cachedValue = configurationManager.Get<TestConfigurationFile>();
        Assert.False( cachedValue.IsModified );
        Assert.Equal( 2, cachedValue.Version );
    }

    /// <summary>
    /// Verifies that a file replaced by an older version by another process can still be updated.
    /// </summary>
    /// <remarks>
    /// This is the counterpart of <see cref="TheCacheDoesNotAdoptAnOlderValue"/>. Because the cache only ever moves
    /// forward, it stays ahead of a file that went backwards, so an update must decide on what the file holds and
    /// not on what the cache holds. Deciding on the cache would decline every attempt, and the caller would retry
    /// until it gave up.
    /// </remarks>
    [Fact]
    public void AFileReplacedByAnOlderVersionCanStillBeUpdated()
    {
        this.Time.Stop();

        using var configurationManager = this.CreateConfigurationManager();

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );
        Assert.True( configurationManager.UpdateIf<TestConfigurationFile>( c => c.IsModified, c => c with { IsModified = false } ) );

        this.WriteFileExternally( configurationManager, new TestConfigurationFile { IsModified = true, Version = 1 } );

        Assert.True( configurationManager.UpdateIf<TestConfigurationFile>( c => c.IsModified, c => c with { IsModified = false } ) );

        var valueFromFile = configurationManager.Get<TestConfigurationFile>( true );
        Assert.False( valueFromFile.IsModified );
        Assert.Equal( 2, valueFromFile.Version );

        Assert.DoesNotContain( this.Log.Entries, e => e.Severity == TestLoggerFactory.Severity.Error );
    }

    /// <summary>
    /// Verifies that two concurrent readers that loaded two different versions of the same file leave the newer of
    /// the two in the cache, whichever of them completes its store first.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// <para>
    /// The synchronization point holds each reader between the moment it decides to replace the cached value and
    /// the replacement itself, which is the window in which the two can be reordered. Both readers are held there
    /// at the same time, so the order in which they are then released is not fixed, and the assertion is
    /// deliberately the one that must hold in either order. The implementation this one replaces stored
    /// unconditionally and therefore left the older version in the cache whenever the older reader happened to
    /// store last.
    /// </para>
    /// <para>
    /// The point is disabled rather than released, because a reader that loses the exchange evaluates the
    /// comparison again and reaches the point a second time.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task ConcurrentReadersLeaveTheNewerVersionInTheCache()
    {
        this.Time.Stop();

        using var configurationManager = this.CreateConfigurationManager();

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = false } ) );

        var syncPointName = Configuration.ConfigurationManager.GetSyncPointName(
            Configuration.ConfigurationManager.AddToCacheBeforeSwapLocation,
            configurationManager.GetFilePath<TestConfigurationFile>() );

        this._syncProvider.EnableSyncPoint( syncPointName );

        this.WriteFileExternally( configurationManager, new TestConfigurationFile { IsModified = false, Version = 2 } );
        var readerOfVersion2 = RunOnDedicatedThreadAsync( () => configurationManager.Get<TestConfigurationFile>( true ) );
        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( syncPointName, this._timeout.Token ) );

        this.WriteFileExternally( configurationManager, new TestConfigurationFile { IsModified = true, Version = 3 } );
        var readerOfVersion3 = RunOnDedicatedThreadAsync( () => configurationManager.Get<TestConfigurationFile>( true ) );
        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( syncPointName, this._timeout.Token ) );

        this._syncProvider.DisableSyncPoint( syncPointName );

        await this.WithTimeout( Task.WhenAll( readerOfVersion2, readerOfVersion3 ) );

        var cachedValue = configurationManager.Get<TestConfigurationFile>();
        Assert.True( cachedValue.IsModified );
        Assert.Equal( 3, cachedValue.Version );
    }

    /// <summary>
    /// Verifies that disposing a manager twice is harmless, and that it does not disturb another manager that is
    /// using the same file.
    /// </summary>
    [Fact]
    public void DisposeIsIdempotentAndLeavesTheOtherManagersAlone()
    {
        var firstManager = this.CreateConfigurationManager();
        using var secondManager = this.CreateConfigurationManager();

        Assert.True( firstManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );

        firstManager.Dispose();
        firstManager.Dispose();

        Assert.True( secondManager.UpdateIf<TestConfigurationFile>( c => c.IsModified, c => c with { IsModified = false } ) );
        Assert.False( secondManager.Get<TestConfigurationFile>( true ).IsModified );
    }
}
