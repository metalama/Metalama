// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Threading;
using Metalama.Testing.Hooks;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.ConfigurationManager;

/// <summary>
/// Tests what <see cref="Configuration.ConfigurationManager"/> does when the machine around it misbehaves: a lock
/// abandoned by a process that terminated, a file that cannot be deserialized, and a manager disposed while another
/// one is using the same file.
/// </summary>
/// <remarks>
/// None of these may fail the operation that happened to be reading or writing a configuration file. The rule
/// throughout is to degrade and carry on, because a configuration file that could not be read or written must never
/// fail a compilation.
/// </remarks>
public sealed class ConfigurationManagerResilienceTests : TestsBase, IDisposable
{
    /// <summary>
    /// Bounds every wait in this class, so that a defect surfaces as a failed assertion rather than as a hung test
    /// run. It is a guard and never a synchronization mechanism.
    /// </summary>
    private readonly CancellationTokenSource _timeout = new( TimeSpan.FromSeconds( 30 ) );

    private readonly TestSynchronizationProvider _syncProvider;

    public ConfigurationManagerResilienceTests( ITestOutputHelper logger ) : base( logger )
    {
        this._syncProvider = new TestSynchronizationProvider( logger.WriteLine );

        this.InitializationOptions = this.InitializationOptions with
        {
            AdditionalJsonTypeInfoResolvers = new IJsonTypeInfoResolver[] { TestConfigurationJsonContext.Default }
        };
    }

    /// <inheritdoc />
    protected override void ConfigureServices( ServiceProviderBuilder services )
        => services.AddService( typeof(ITestSynchronizationProvider), this._syncProvider );

    /// <inheritdoc />
    public void Dispose()
    {
        this._syncProvider.Dispose();
        this._timeout.Dispose();
    }

    private Configuration.ConfigurationManager CreateConfigurationManager() => new( this.ServiceProvider );

    private static string GetLockName( IConfigurationManager configurationManager )
        => NamedLockExtensions.GetGlobalLockName( configurationManager.GetFilePath<TestConfigurationFile>() );

    /// <summary>
    /// Runs an action on a thread of its own, so that a test can drive it while it is blocked.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns>A task that completes when the action returns.</returns>
    /// <remarks>
    /// No cancellation token is passed to the scheduler on purpose: a token that is already signalled makes the
    /// delegate never run, which would leave the signals awaited by the caller unset.
    /// </remarks>
    private static Task<T> RunOnDedicatedThreadAsync<T>( Func<T> action )
        => Task.Factory.StartNew( action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default );

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
    /// Verifies that a lock abandoned by a process that terminated without releasing it is acquired rather than
    /// making the update fail.
    /// </summary>
    /// <remarks>
    /// A process killed while holding the lock is ordinary during a build, and the state it protects is a file that
    /// is written atomically, so an abandoned lock leaves nothing half done. Refusing to take it would leave every
    /// later process unable to write the file.
    /// </remarks>
    [Fact]
    public void AnAbandonedLockDoesNotPreventAnUpdate()
    {
        using var configurationManager = this.CreateConfigurationManager();

        this.Locks.Abandon( GetLockName( configurationManager ) );

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );
        Assert.True( configurationManager.Get<TestConfigurationFile>( true ).IsModified );
    }

    /// <summary>
    /// Verifies that a file that cannot be deserialized, left behind by a process that was killed while writing it
    /// with an implementation that did not write atomically, is replaced rather than making the update fail.
    /// </summary>
    /// <remarks>
    /// The lock is abandoned as well, which is the state such a file is found in: the process that was writing it
    /// held the lock and did not release it.
    /// </remarks>
    [Fact]
    public void AnAbandonedLockOverAnUnreadableFileDoesNotPreventAnUpdate()
    {
        using var configurationManager = this.CreateConfigurationManager();
        var path = configurationManager.GetFilePath<TestConfigurationFile>();

        this.FileSystem.WriteAllText( path, "{ this is not the whole file" );
        this.Locks.Abandon( GetLockName( configurationManager ) );

        // The read recovers with a default instance carrying the timestamp of the file, so the update has a base
        // to work from rather than looping on a file that seems not to exist.
        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );

        var value = configurationManager.Get<TestConfigurationFile>( true );
        Assert.True( value.IsModified );
        Assert.Equal( 1, value.Version );

        Assert.Contains( this.Log.Entries, e => e.Severity == TestLoggerFactory.Severity.Error );
    }

    /// <summary>
    /// Verifies that a handler never observes a version of a configuration file older than the one it was handed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the guarantee that replaces the one dispatching inside the lock would have given. It is the weak
    /// one: a notification means that the file has changed and is worth re-reading, not that the value handed over
    /// is the current content. What is guaranteed is that re-reading never takes the handler backwards, which is
    /// what the forward-only cache provides and what a handler actually depends on.
    /// </para>
    /// <para>
    /// A handler that observed an older version than the one it was handed could undo a newer setting with an older
    /// one, which is the failure this excludes.
    /// </para>
    /// <para>
    /// The interleaving is forced rather than raced for. Both writers are held between the release of the lock and
    /// the invocation of the handlers, so that by the time either handler runs, both writes have been made and the
    /// cache holds the second one. The handler of the first write is therefore certain to be handed a value the
    /// cache has already moved past, which is the only situation in which the guarantee says anything at all.
    /// Letting two writers race instead produced that situation zero times in a hundred notifications.
    /// </para>
    /// </remarks>
    /// <returns>A task that completes when the test does.</returns>
    [Fact]
    public async Task AHandlerNeverObservesAnOlderVersionThanTheOneItWasHanded()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.False( configurationManager.Get<TestConfigurationFile>().IsModified );

        var violations = new ConcurrentQueue<string>();
        var notifications = 0;
        var sawTheCacheAhead = 0;

        configurationManager.ConfigurationFileChanged += value =>
        {
            Interlocked.Increment( ref notifications );

            var handedVersion = value.Version ?? 0;
            var observedVersion = configurationManager.Get<TestConfigurationFile>().Version ?? 0;

            if ( observedVersion < handedVersion )
            {
                violations.Enqueue( $"Handed version {handedVersion} but observed {observedVersion}." );
            }
            else if ( observedVersion > handedVersion )
            {
                Interlocked.Increment( ref sawTheCacheAhead );
            }
        };

        var beforeInvokeSyncPoint = Configuration.ConfigurationManager.GetSyncPointName(
            Configuration.ConfigurationManager.RaiseChangedBeforeInvokeLocation,
            configurationManager.GetFilePath<TestConfigurationFile>() );

        this._syncProvider.EnableSyncPoint( beforeInvokeSyncPoint );

        var writers = new Task[2];

        for ( var writerId = 0; writerId < writers.Length; writerId++ )
        {
            var mark = writerId.ToString( CultureInfo.InvariantCulture );

            writers[writerId] = RunOnDedicatedThreadAsync(
                () => configurationManager.Update<TestConfigurationFile>( c => c with { Marks = c.Marks + mark } ) );

            // Waiting for each writer to reach the point before starting the next one orders the two writes, and
            // leaves the first writer holding a value that the second one supersedes.
            await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( beforeInvokeSyncPoint, this._timeout.Token ) );
        }

        // Both writes are done and the cache holds the second one. Now let the handlers run.
        this._syncProvider.DisableSyncPoint( beforeInvokeSyncPoint );

        await this.WithTimeout( Task.WhenAll( writers ) );

        Assert.Equal( 2, notifications );
        Assert.Empty( violations );

        // The situation the guarantee is about was actually reached, rather than the assertion holding vacuously.
        Assert.Equal( 1, sawTheCacheAhead );
    }

    /// <summary>
    /// Verifies that disposing one manager does not disturb another one that is using the same file at that
    /// moment.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// Each manager owns its own handles on the locks it uses, so disposing one must release its handles and
    /// nothing else. Disposing the underlying object while another manager was waiting on it would leave that
    /// manager waiting on a handle that no longer exists.
    /// </remarks>
    [Fact]
    public async Task DisposingOneManagerDoesNotDisturbAnotherOneHoldingTheSameLock()
    {
        using var holdingManager = this.CreateConfigurationManager();
        var otherManager = this.CreateConfigurationManager();

        var beforeUnlockSyncPoint = Configuration.ConfigurationManager.GetSyncPointName(
            Configuration.ConfigurationManager.UpdateBeforeUnlockLocation,
            holdingManager.GetFilePath<TestConfigurationFile>() );

        this._syncProvider.EnableSyncPoint( beforeUnlockSyncPoint );

        var update = RunOnDedicatedThreadAsync(
            () => holdingManager.Update(
                typeof(TestConfigurationFile),
                currentValue => ((TestConfigurationFile) currentValue) with { IsModified = true } ) );

        // The first manager has written the file and still holds the lock.
        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( beforeUnlockSyncPoint, this._timeout.Token ) );

        // Disposed twice, so that idempotence is exercised at the least convenient moment.
        otherManager.Dispose();
        otherManager.Dispose();

        this._syncProvider.DisableSyncPoint( beforeUnlockSyncPoint );

        Assert.Equal( ConfigurationUpdateOutcome.Updated, await update );

        Assert.Empty( this.Locks.GetHeldLocks() );
        Assert.True( holdingManager.Get<TestConfigurationFile>( true ).IsModified );
    }
}
