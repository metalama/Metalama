// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Threading;
using Metalama.Testing.Hooks;
using System;
using System.IO;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.ConfigurationManager;

/// <summary>
/// Tests that reading, transforming and writing a configuration file is one transaction.
/// </summary>
/// <remarks>
/// The property under test is that the value a transformation receives is the content of the file at the moment of
/// the write. The implementation this one replaces read outside the lock, wrote inside it and compared timestamps
/// to detect that the two had diverged, which cost one acquisition, one read and one serialization per attempt and
/// gave up after ten.
/// </remarks>
public sealed class ConfigurationManagerTransactionTests : TestsBase, IDisposable
{
    /// <summary>
    /// Bounds every wait in this class, so that a defect surfaces as a failed assertion rather than as a hung test
    /// run. It is a guard and never a synchronization mechanism.
    /// </summary>
    private readonly CancellationTokenSource _timeout = new( TimeSpan.FromSeconds( 30 ) );

    private readonly TestSynchronizationProvider _syncProvider;

    public ConfigurationManagerTransactionTests( ITestOutputHelper logger ) : base( logger )
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

    private static string GetSyncPointName( IConfigurationManager configurationManager, string location )
        => Configuration.ConfigurationManager.GetSyncPointName( location, configurationManager.GetFilePath<TestConfigurationFile>() );

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
    /// Appends a mark to the accumulating record of the test configuration file.
    /// </summary>
    /// <param name="configurationManager">The manager.</param>
    /// <param name="mark">The mark to append.</param>
    /// <returns>The outcome of the update.</returns>
    private static ConfigurationUpdateOutcome AppendMark( IConfigurationManager configurationManager, string mark )
        => configurationManager.Update(
            typeof(TestConfigurationFile),
            currentValue => ((TestConfigurationFile) currentValue) with { Marks = ((TestConfigurationFile) currentValue).Marks + mark } );

    /// <summary>
    /// Verifies that an update acquires the lock protecting the file exactly once.
    /// </summary>
    /// <remarks>
    /// The optimistic loop this replaces acquired it once per attempt, and re-read and re-serialized the file each
    /// time.
    /// </remarks>
    [Fact]
    public void AnUpdateAcquiresTheLockExactlyOnce()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.Equal( ConfigurationUpdateOutcome.Updated, AppendMark( configurationManager, "a" ) );

        Assert.Equal( 1, this.Locks.GetAcquisitionCount( GetLockName( configurationManager ) ) );
    }

    /// <summary>
    /// Verifies that two updates of the same file that overlap both take effect, and that they take the lock once
    /// each.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// <para>
    /// The first writer is held inside the lock, between the read and the write, which is the whole window in which
    /// the second writer could have based its own transformation on a value that is about to be superseded. It
    /// cannot, because it has not read anything yet: it is waiting for the lock.
    /// </para>
    /// <para>
    /// The record accumulated in the file is what makes a lost update visible. Counting the successful updates
    /// would not: a lost update leaves the count right and the content wrong.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task OverlappingUpdatesOfOneFileBothTakeEffect()
    {
        using var configurationManager = this.CreateConfigurationManager();

        var afterReadSyncPoint = GetSyncPointName( configurationManager, Configuration.ConfigurationManager.UpdateAfterReadLocation );
        this._syncProvider.EnableSyncPoint( afterReadSyncPoint );

        var firstWriter = RunOnDedicatedThreadAsync( () => AppendMark( configurationManager, "a" ) );

        // The first writer holds the lock and has read the file.
        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( afterReadSyncPoint, this._timeout.Token ) );

        // The second writer cannot read anything until the first one has released the lock.
        var secondWriter = RunOnDedicatedThreadAsync( () => AppendMark( configurationManager, "b" ) );
        await this.WithTimeout( this.Locks.WaitForWaitersAsync( GetLockName( configurationManager ), 1, this._timeout.Token ) );

        this._syncProvider.DisableSyncPoint( afterReadSyncPoint );

        await this.WithTimeout( Task.WhenAll( firstWriter, secondWriter ) );

        Assert.Equal( ConfigurationUpdateOutcome.Updated, await firstWriter );
        Assert.Equal( ConfigurationUpdateOutcome.Updated, await secondWriter );

        var value = configurationManager.Get<TestConfigurationFile>( true );
        Assert.Equal( "ab", value.Marks );
        Assert.Equal( 2, value.Version );

        // One acquisition each, and no attempt was ever abandoned and retried.
        Assert.Equal( 2, this.Locks.GetAcquisitionCount( GetLockName( configurationManager ) ) );
    }

    /// <summary>
    /// Verifies that a transformation that attempts to update another configuration file is refused.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the price of running the transformation inside the lock, and the reason the price is acceptable: the
    /// one thing a transformation must not do is detected and reported, instead of producing a thread that holds
    /// two named locks and deadlocks against another thread that takes the same two in the opposite order.
    /// </para>
    /// <para>
    /// Reading another configuration file from a transformation remains allowed, because a read takes no lock.
    /// </para>
    /// </remarks>
    [Fact]
    public void ATransformationCannotUpdateAnotherFile()
    {
        using var configurationManager = this.CreateConfigurationManager();

        var exception = Assert.Throws<InvalidOperationException>(
            () => configurationManager.Update(
                typeof(TestConfigurationFile),
                currentValue =>
                {
                    // Reading is allowed and must not throw.
                    _ = configurationManager.Get<SecondTestConfigurationFile>();

                    configurationManager.Update<SecondTestConfigurationFile>( c => c with { IsModified = true } );

                    return currentValue;
                } ) );

        Assert.Contains( "not reentrant", exception.Message, StringComparison.Ordinal );

        // The lock of the outer file was released despite the exception.
        Assert.Empty( this.Locks.GetHeldLocks() );
    }

    /// <summary>
    /// Verifies that a transformation that declines leaves the file untouched and takes no further action.
    /// </summary>
    [Fact]
    public void ADecliningTransformationWritesNothing()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.Equal( ConfigurationUpdateOutcome.Updated, AppendMark( configurationManager, "a" ) );

        Assert.Equal(
            ConfigurationUpdateOutcome.Declined,
            configurationManager.Update( typeof(TestConfigurationFile), _ => null ) );

        var value = configurationManager.Get<TestConfigurationFile>( true );
        Assert.Equal( "a", value.Marks );
        Assert.Equal( 1, value.Version );
    }

    /// <summary>
    /// Verifies that a transformation producing the value the file already holds is reported as such and does not
    /// write.
    /// </summary>
    [Fact]
    public void ATransformationThatChangesNothingWritesNothing()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.Equal( ConfigurationUpdateOutcome.Updated, AppendMark( configurationManager, "a" ) );

        Assert.Equal(
            ConfigurationUpdateOutcome.NoChange,
            configurationManager.Update( typeof(TestConfigurationFile), currentValue => currentValue ) );

        Assert.Equal( 1, configurationManager.Get<TestConfigurationFile>( true ).Version );
    }

    /// <summary>
    /// Verifies that a condition that does not hold prevents the lock from being acquired at all.
    /// </summary>
    /// <remarks>
    /// This is the point of evaluating the condition before taking the lock. Most conditions in the product ask
    /// whether a setting is already in the desired state, and they hold on the first call and never again, so the
    /// great majority of calls must stop here.
    /// </remarks>
    [Fact]
    public void UpdateIfTakesNoLockWhenTheConditionDoesNotHold()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.True( configurationManager.UpdateIf<TestConfigurationFile>( c => !c.IsModified, c => c with { IsModified = true } ) );

        var acquisitionsAfterFirstCall = this.Locks.GetAcquisitionCount( GetLockName( configurationManager ) );

        Assert.False( configurationManager.UpdateIf<TestConfigurationFile>( c => !c.IsModified, c => c with { IsModified = true } ) );

        Assert.Equal( acquisitionsAfterFirstCall, this.Locks.GetAcquisitionCount( GetLockName( configurationManager ) ) );
    }

    /// <summary>
    /// Verifies that the condition of an update is evaluated again inside the transaction, so that a condition that
    /// stopped holding while the lock was being waited for does not lead to a write.
    /// </summary>
    /// <returns>A task that completes when the test does.</returns>
    /// <remarks>
    /// The evaluation before the lock is only a filter. This is the evaluation that decides, and it is what makes
    /// the check and the write atomic with respect to each other.
    /// </remarks>
    [Fact]
    public async Task UpdateIfEvaluatesTheConditionAgainInsideTheTransaction()
    {
        using var configurationManager = this.CreateConfigurationManager();

        var afterReadSyncPoint = GetSyncPointName( configurationManager, Configuration.ConfigurationManager.UpdateAfterReadLocation );
        this._syncProvider.EnableSyncPoint( afterReadSyncPoint );

        var firstWriter = RunOnDedicatedThreadAsync(
            () => configurationManager.UpdateIf<TestConfigurationFile>( c => !c.IsModified, c => c with { IsModified = true } ) );

        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( afterReadSyncPoint, this._timeout.Token ) );

        // The second caller evaluates its condition on a file that is still unmodified, so it passes the filter and
        // proceeds to wait for the lock.
        var secondWriter = RunOnDedicatedThreadAsync(
            () => configurationManager.UpdateIf<TestConfigurationFile>( c => !c.IsModified, c => c with { IsModified = true } ) );

        await this.WithTimeout( this.Locks.WaitForWaitersAsync( GetLockName( configurationManager ), 1, this._timeout.Token ) );

        this._syncProvider.DisableSyncPoint( afterReadSyncPoint );

        await this.WithTimeout( Task.WhenAll( firstWriter, secondWriter ) );

        Assert.True( await firstWriter );

        // The condition no longer holds by the time the second caller owns the lock, so it writes nothing.
        Assert.False( await secondWriter );

        Assert.Equal( 1, configurationManager.Get<TestConfigurationFile>( true ).Version );
    }

    /// <summary>
    /// Verifies that a write that fails leaves the previous content of the file, and the cached value, exactly as
    /// they were.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The write is atomic, so a failure cannot have left the file half written. What this checks is the rest: that
    /// the failure is reported rather than raised, that nothing was recorded as though the write had succeeded, and
    /// that the lock was released.
    /// </para>
    /// <para>
    /// The failure is injected on the operation rather than with <see cref="TestFileSystem.BlockWrite"/>, because
    /// the latter fails the reads of the path as well as its writes, and the read of this transaction is retried
    /// and would absorb it.
    /// </para>
    /// </remarks>
    [Fact]
    public void AFailedWriteLeavesThePreviousContentIntact()
    {
        using var configurationManager = this.CreateConfigurationManager();
        var path = configurationManager.GetFilePath<TestConfigurationFile>();

        Assert.Equal( ConfigurationUpdateOutcome.Updated, AppendMark( configurationManager, "a" ) );

        this.FileSystem.SetEvent(
            nameof(IFileSystem.WriteAllTextAtomically),
            path,
            () => throw new IOException( "Injected by a test." ) );

        Assert.Equal( ConfigurationUpdateOutcome.WriteFailed, AppendMark( configurationManager, "b" ) );

        this.FileSystem.ResetEvent( nameof(IFileSystem.WriteAllTextAtomically), path );

        var value = configurationManager.Get<TestConfigurationFile>( true );
        Assert.Equal( "a", value.Marks );
        Assert.Equal( 1, value.Version );

        // The cache was not moved on either.
        Assert.Equal( "a", configurationManager.Get<TestConfigurationFile>().Marks );

        Assert.Empty( this.Locks.GetHeldLocks() );
    }
}
