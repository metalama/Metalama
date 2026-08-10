// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Serialization;
using Metalama.Backstage.Testing;
using Metalama.Testing.Hooks;
using System;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.ConfigurationManager;

/// <summary>
/// Tests how <see cref="Configuration.ConfigurationManager"/> processes the changes made to its files by another
/// process, which it learns about from a file system watcher.
/// </summary>
/// <remarks>
/// This is the path that made issue 1847 as damaging as it was. The implementation this one replaces held the
/// single lock of the whole data directory across the entire processing of the pending changes, and ran that
/// processing twice, so a process that both writes the configuration and watches it blocked its own writers for as
/// long as notifications kept arriving. The compiler server is exactly such a process.
/// </remarks>
public sealed class ConfigurationManagerWatcherTests : TestsBase, IDisposable
{
    /// <summary>
    /// Bounds every wait in this class, so that a defect surfaces as a failed assertion rather than as a hung test
    /// run. It is a guard and never a synchronization mechanism.
    /// </summary>
    private readonly CancellationTokenSource _timeout = new( TimeSpan.FromSeconds( 30 ) );

    private readonly TestSynchronizationProvider _syncProvider;

    public ConfigurationManagerWatcherTests( ITestOutputHelper logger ) : base(
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
        => services.AddService( typeof(ITestSynchronizationProvider), this._syncProvider );

    /// <inheritdoc />
    public void Dispose()
    {
        this._syncProvider.Dispose();
        this._timeout.Dispose();
    }

    private Configuration.ConfigurationManager CreateConfigurationManager() => new( this.ServiceProvider );

    /// <summary>
    /// Writes a configuration file directly, as another process that does not go through the manager would.
    /// </summary>
    /// <param name="configurationManager">The manager, used only to locate the file.</param>
    /// <param name="value">The value to write, whose <see cref="ConfigurationFile.Version"/> must be set explicitly.</param>
    private void WriteFileExternally( IConfigurationManager configurationManager, TestConfigurationFile value )
    {
        var json = this.ServiceProvider.GetRequiredBackstageService<IJsonSerializationService>()
            .Serialize( value, typeof(TestConfigurationFile) );

        this.FileSystem.WriteAllText( configurationManager.GetFilePath<TestConfigurationFile>(), json );
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
    /// Returns a task that completes when a configuration file satisfying a predicate is announced.
    /// </summary>
    /// <param name="configurationManager">The manager to subscribe to.</param>
    /// <param name="predicate">Decides whether an announced value is the one being waited for.</param>
    /// <returns>A task that completes when such a value is announced.</returns>
    private static Task WaitForChangeAsync( IConfigurationManager configurationManager, Predicate<TestConfigurationFile> predicate )
    {
        var signal = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        configurationManager.ConfigurationFileChanged += value =>
        {
            if ( value is TestConfigurationFile testValue && predicate( testValue ) )
            {
                signal.TrySetResult( true );
            }
        };

        return signal.Task;
    }

    /// <summary>
    /// Verifies that an update completes while the processing of an external change is in progress.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// <para>
    /// This is the starvation regression. The processing is held at the point where it has taken a file from the
    /// pending changes and is about to reload it, which is the middle of what used to be one long critical section,
    /// and an unrelated writer of the very same file must nonetheless complete.
    /// </para>
    /// <para>
    /// A notification that arrives while the processing is held starts another pass rather than being dropped,
    /// which is why the synchronization point is disabled rather than released at the end: the number of further
    /// passes is not known.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task AnUpdateCompletesWhileAnExternalChangeIsBeingProcessed()
    {
        using var configurationManager = this.CreateConfigurationManager();
        var path = configurationManager.GetFilePath<TestConfigurationFile>();

        // The file must be in the cache, otherwise the watcher considers the change to concern no known file.
        Assert.Null( configurationManager.Get<TestConfigurationFile>().Timestamp );

        var afterDequeueSyncPoint = Configuration.ConfigurationManager.GetSyncPointName(
            Configuration.ConfigurationManager.ProcessFileChangesAfterDequeueLocation,
            path );

        this._syncProvider.EnableSyncPoint( afterDequeueSyncPoint );

        // Another process writes the file, which the watcher notices.
        this.WriteFileExternally( configurationManager, new TestConfigurationFile { Marks = "external", Version = 1 } );

        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( afterDequeueSyncPoint, this._timeout.Token ) );

        // The processing holds no lock, so this completes rather than waiting for it.
        Assert.Equal(
            ConfigurationUpdateOutcome.Updated,
            configurationManager.Update(
                typeof(TestConfigurationFile),
                currentValue => ((TestConfigurationFile) currentValue) with { IsModified = true } ) );

        Assert.Empty( this.Locks.GetHeldLocks() );

        this._syncProvider.DisableSyncPoint( afterDequeueSyncPoint );
    }

    /// <summary>
    /// Verifies that the processing of an external change terminates, and leaves the cache holding the content of
    /// the file.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    [Fact]
    public async Task AnExternalChangeReachesTheCache()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.Null( configurationManager.Get<TestConfigurationFile>().Timestamp );

        var changed = WaitForChangeAsync( configurationManager, value => value.Marks == "external" );

        this.WriteFileExternally( configurationManager, new TestConfigurationFile { Marks = "external", Version = 1 } );

        await this.WithTimeout( changed );

        Assert.Equal( "external", configurationManager.Get<TestConfigurationFile>().Marks );
    }

    /// <summary>
    /// Verifies that a change notified while a pass is in progress is not dropped.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// <para>
    /// The pending state is released before the work rather than after it, so a notification arriving mid-pass
    /// either merges into an entry that has not been taken yet or arms a fresh pass. Moving that release after the
    /// drain would reopen the window in which a notification is silently lost, and this is the test that would
    /// then fail.
    /// </para>
    /// <para>
    /// The pass must be held <em>after</em> it has taken the file from the pending changes, not before. Held
    /// before, the second notification merely merges into an entry that has not been taken yet, and the same pass
    /// reloads the newer content on its own: the test would pass whichever way the flag was ordered. It is only
    /// once the entry has been taken that a second notification has to arm a pass of its own, which it can do only
    /// if the flag has already been released.
    /// </para>
    /// <para>
    /// Reaching the synchronization point a second time is therefore the assertion. Under the reversed ordering no
    /// second pass is armed at all and the wait below times out.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task AChangeNotifiedDuringAPassIsNotDropped()
    {
        using var configurationManager = this.CreateConfigurationManager();
        var path = configurationManager.GetFilePath<TestConfigurationFile>();

        Assert.Null( configurationManager.Get<TestConfigurationFile>().Timestamp );

        var afterDequeueSyncPoint = Configuration.ConfigurationManager.GetSyncPointName(
            Configuration.ConfigurationManager.ProcessFileChangesAfterDequeueLocation,
            path );

        this._syncProvider.EnableSyncPoint( afterDequeueSyncPoint );

        this.WriteFileExternally( configurationManager, new TestConfigurationFile { Marks = "first", Version = 1 } );

        // The first pass has taken the file from the pending changes and has not reloaded it yet.
        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( afterDequeueSyncPoint, this._timeout.Token ) );

        var secondChangeSeen = WaitForChangeAsync( configurationManager, value => value.Marks == "second" );
        this.WriteFileExternally( configurationManager, new TestConfigurationFile { Marks = "second", Version = 2 } );

        // A second pass was armed, which is what the released flag makes possible.
        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( afterDequeueSyncPoint, this._timeout.Token ) );

        this._syncProvider.DisableSyncPoint( afterDequeueSyncPoint );

        await this.WithTimeout( secondChangeSeen );

        Assert.Equal( "second", configurationManager.Get<TestConfigurationFile>().Marks );
    }

    /// <summary>
    /// Verifies that two managers over one file system see each other's writes.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// Two managers over one directory are two processes as far as the locks and the files are concerned, so this
    /// is the in-process form of the situation the whole design exists for.
    /// </remarks>
    [Fact]
    public async Task TwoManagersSeeEachOthersWrites()
    {
        using var firstManager = this.CreateConfigurationManager();
        using var secondManager = this.CreateConfigurationManager();

        // Both have read the file, so both are watching it.
        Assert.Null( firstManager.Get<TestConfigurationFile>().Timestamp );
        Assert.Null( secondManager.Get<TestConfigurationFile>().Timestamp );

        var secondManagerNotified = WaitForChangeAsync( secondManager, value => value.IsModified );

        Assert.True( firstManager.Update<TestConfigurationFile>( c => c with { IsModified = true, Marks = "first" } ) );

        // Reading the file gives the second manager the new content at once, without waiting for a notification.
        Assert.True( secondManager.Get<TestConfigurationFile>( true ).IsModified );

        // And the notification brings the same value to its cache.
        await this.WithTimeout( secondManagerNotified );

        var cachedValue = secondManager.Get<TestConfigurationFile>();
        Assert.True( cachedValue.IsModified );
        Assert.Equal( "first", cachedValue.Marks );
    }
}
