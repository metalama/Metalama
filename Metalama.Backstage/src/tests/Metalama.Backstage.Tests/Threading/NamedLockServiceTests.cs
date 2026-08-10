// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Threading;
using Metalama.Testing.Hooks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Threading;

/// <summary>
/// Tests <see cref="NamedLockService"/> against the real synchronization objects of the operating system.
/// </summary>
/// <remarks>
/// <para>
/// These tests use real objects rather than a substitute, because the point of this class is precisely the way it
/// uses the operating system. They are nonetheless fast and deterministic, and are therefore not excluded from
/// continuous integration: every wait is released by another thread of the same process rather than by the passage
/// of time.
/// </para>
/// <para>
/// The names are unique per test and use the <c>Local\</c> prefix rather than <c>Global\</c>, so that concurrent
/// test runs, and any Metalama process that happens to run on the same machine, cannot interfere.
/// </para>
/// </remarks>
public sealed class NamedLockServiceTests : IDisposable
{
    /// <summary>
    /// Bounds every wait in this class, so that a defect surfaces as a failed assertion rather than as a hung
    /// test run. It is a guard and never a synchronization mechanism: no test depends on its duration.
    /// </summary>
    private readonly CancellationTokenSource _timeout = new( TimeSpan.FromSeconds( 30 ) );

    private readonly ITestOutputHelper _logger;
    private readonly object _eventsSync = new();
    private readonly List<LockEventArgs> _events = new();

    /// <summary>
    /// Drives the synchronization points of <see cref="NamedLockService"/>. A point that a test has not armed is
    /// a no-op, so this can be supplied to every service without affecting the tests that ignore it.
    /// </summary>
    private readonly TestSynchronizationProvider _syncProvider;

    /// <summary>
    /// Signals armed by <see cref="WaitForEventAsync"/>, so that a test can wait for the code under test to reach
    /// a given state instead of waiting for a duration.
    /// </summary>
    private readonly List<(LockEventKind Kind, string Name, TaskCompletionSource<bool> Signal)> _eventWaiters = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NamedLockServiceTests"/> class.
    /// </summary>
    /// <param name="logger">The xunit output helper.</param>
    public NamedLockServiceTests( ITestOutputHelper logger )
    {
        this._logger = logger;
        this._syncProvider = new TestSynchronizationProvider( logger.WriteLine );
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Releasing every point first guarantees that no dedicated thread is left pinned, which would otherwise
        // keep a named lock held for the rest of the test run.
        this._syncProvider.Dispose();
        this._timeout.Dispose();
    }

    /// <summary>
    /// Creates a service whose events are recorded by this test and whose synchronization points this test can
    /// arm.
    /// </summary>
    /// <returns>The service.</returns>
    private NamedLockService CreateService()
    {
        var service = new NamedLockService( new TestServiceProvider( this._syncProvider ) );
        service.LockEventReported += this.OnLockEvent;

        return service;
    }

    /// <summary>
    /// The minimal service provider through which <see cref="NamedLockService"/> resolves the synchronization
    /// points.
    /// </summary>
    /// <remarks>
    /// <see cref="ITestSynchronizationProvider"/> is resolved untyped, because it is shared with the layers above
    /// <c>Metalama.Backstage</c> and therefore derives from no dependency injection marker interface.
    /// </remarks>
    private sealed class TestServiceProvider : IServiceProvider
    {
        private readonly TestSynchronizationProvider _syncProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestServiceProvider"/> class.
        /// </summary>
        /// <param name="syncProvider">The provider to return.</param>
        public TestServiceProvider( TestSynchronizationProvider syncProvider )
        {
            this._syncProvider = syncProvider;
        }

        /// <inheritdoc />
        public object? GetService( Type serviceType ) => serviceType == typeof(ITestSynchronizationProvider) ? this._syncProvider : null;
    }

    /// <summary>
    /// Records an event and releases any waiter armed for it.
    /// </summary>
    /// <param name="sender">The service that reported the event.</param>
    /// <param name="lockEvent">The event.</param>
    private void OnLockEvent( object? sender, LockEventArgs lockEvent )
    {
        this._logger.WriteLine( lockEvent.ToString() );

        lock ( this._eventsSync )
        {
            this._events.Add( lockEvent );

            foreach ( var waiter in this._eventWaiters )
            {
                if ( waiter.Kind == lockEvent.Kind && waiter.Name == lockEvent.Name )
                {
                    waiter.Signal.TrySetResult( true );
                }
            }
        }
    }

    /// <summary>
    /// Gets a snapshot of the events recorded so far.
    /// </summary>
    /// <returns>The events, in order.</returns>
    private List<LockEventArgs> GetEvents()
    {
        lock ( this._eventsSync )
        {
            return this._events.ToList();
        }
    }

    /// <summary>
    /// Returns a task that completes when a given event is reported, arming the waiter before the caller starts
    /// the work that is expected to report it.
    /// </summary>
    /// <param name="kind">The kind of event to wait for.</param>
    /// <param name="name">The name of the lock.</param>
    /// <returns>The task.</returns>
    private Task WaitForEventAsync( LockEventKind kind, string name )
    {
        var signal = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        lock ( this._eventsSync )
        {
            // An event reported before the waiter was armed still counts, otherwise arming would be a race.
            if ( this._events.Any( e => e.Kind == kind && e.Name == name ) )
            {
                signal.TrySetResult( true );
            }
            else
            {
                this._eventWaiters.Add( (kind, name, signal) );
            }
        }

        return this.WithTimeout( signal.Task );
    }

    /// <summary>
    /// Creates a lock name that no other test and no other process can be using.
    /// </summary>
    /// <returns>The name.</returns>
    private static string CreateName() => "Local\\MetalamaTest_" + Guid.NewGuid().ToString( "N", CultureInfo.InvariantCulture );

    /// <summary>
    /// Runs an action on a thread of its own, which is required because a named lock has thread affinity and must
    /// be released by the thread that acquired it.
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
    /// Holds a named lock on a thread of its own, for as long as the test wants.
    /// </summary>
    /// <remarks>
    /// A named lock has thread affinity, so it must be released by the thread that acquired it. A test that
    /// acquired a lock on the xunit thread and released it after an <c>await</c> would release it from whichever
    /// thread happened to run the continuation, which raises <see cref="ApplicationException"/>. This class
    /// confines the whole lifetime of an acquisition to one dedicated thread, and lets the test drive it with
    /// signals rather than with delays.
    /// </remarks>
    private sealed class LockHolder
    {
        private readonly TaskCompletionSource<bool> _acquired = new( TaskCreationOptions.RunContinuationsAsynchronously );
        private readonly TaskCompletionSource<bool> _release = new( TaskCreationOptions.RunContinuationsAsynchronously );

        /// <summary>
        /// Initializes a new instance of the <see cref="LockHolder"/> class and starts acquiring the lock.
        /// </summary>
        /// <param name="lock">The lock to acquire.</param>
        /// <param name="timeout">The timeout passed to <see cref="INamedLock.TryAcquire"/>.</param>
        /// <param name="cancellationToken">The token passed to <see cref="INamedLock.TryAcquire"/>.</param>
        public LockHolder( INamedLock @lock, TimeSpan timeout, CancellationToken cancellationToken = default )
        {
            this.Completed = RunOnDedicatedThreadAsync(
                () =>
                {
                    IDisposable releaser;

                    try
                    {
                        if ( !@lock.TryAcquire( timeout, out var acquiredReleaser, cancellationToken ) )
                        {
                            this._acquired.TrySetResult( false );

                            return;
                        }

                        releaser = acquiredReleaser;
                    }
                    catch ( Exception e )
                    {
                        // Faulting the task rather than recording the exception, so that a test that forgets to
                        // assert on it fails rather than passing silently.
                        this._acquired.TrySetException( e );

                        return;
                    }

                    this._acquired.TrySetResult( true );

                    this._release.Task.GetAwaiter().GetResult();

                    releaser.Dispose();
                } );
        }

        /// <summary>
        /// Gets a task whose result indicates whether the lock was acquired.
        /// </summary>
        public Task<bool> Acquired => this._acquired.Task;

        /// <summary>
        /// Gets a task that completes once the lock has been released and the thread has finished.
        /// </summary>
        public Task Completed { get; }

        /// <summary>
        /// Tells the dedicated thread to release the lock.
        /// </summary>
        public void Release() => this._release.TrySetResult( true );
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
    /// Awaits a task and returns its result, failing rather than hanging if <see cref="_timeout"/> elapses first.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="task">The task to await.</param>
    /// <returns>The result of <paramref name="task"/>.</returns>
    private async Task<T> WithTimeout<T>( Task<T> task )
    {
        await this.WithTimeout( (Task) task );

        return await task;
    }

    [Fact]
    public void UncontendedAcquisition_ReportsCreatedAcquiredAndReleased()
    {
        var name = CreateName();
        var service = this.CreateService();

        using ( var @lock = service.GetLock( name ) )
        {
            Assert.Equal( name, @lock.Name );

            Assert.True( @lock.TryAcquire( TimeSpan.Zero, out var handle ) );

            Assert.NotNull( handle );
            handle!.Dispose();
        }

        var kinds = this.GetEvents().Where( e => e.Name == name ).Select( e => e.Kind ).ToList();

        Assert.Equal( new[] { LockEventKind.Created, LockEventKind.Acquired, LockEventKind.Released }, kinds );
    }

    [Fact]
    public async Task SecondThread_BlocksUntilTheFirstReleases()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var firstLock = service.GetLock( name );
        using var secondLock = service.GetLock( name );

        var firstHolder = new LockHolder( firstLock, TimeSpan.Zero );
        Assert.True( await this.WithTimeout( firstHolder.Acquired ) );

        var blocked = this.WaitForEventAsync( LockEventKind.Blocked, name );
        var secondHolder = new LockHolder( secondLock, Timeout.InfiniteTimeSpan );

        // The Blocked event is the proof that the second thread is genuinely waiting for the lock, which is what
        // makes the assertion below meaningful rather than a race that happens to pass.
        await blocked;

        Assert.False( secondHolder.Acquired.IsCompleted );

        firstHolder.Release();

        Assert.True( await this.WithTimeout( secondHolder.Acquired ) );

        secondHolder.Release();
        await this.WithTimeout( Task.WhenAll( firstHolder.Completed, secondHolder.Completed ) );
    }

    [Fact]
    public async Task ZeroTimeoutOnAnOwnedLock_ReturnsNullAndReportsTimedOut()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var ownerLock = service.GetLock( name );
        using var contenderLock = service.GetLock( name );

        var owner = new LockHolder( ownerLock, TimeSpan.Zero );
        Assert.True( await this.WithTimeout( owner.Acquired ) );

        var contender = new LockHolder( contenderLock, TimeSpan.Zero );

        Assert.False( await this.WithTimeout( contender.Acquired ) );
        Assert.Contains( this.GetEvents(), e => e.Kind == LockEventKind.TimedOut && e.Name == name );

        owner.Release();
        await this.WithTimeout( owner.Completed );
    }

    [Fact]
    public async Task DifferentNames_DoNotBlockEachOther()
    {
        var firstName = CreateName();
        var secondName = CreateName();
        var service = this.CreateService();

        using var firstLock = service.GetLock( firstName );
        using var secondLock = service.GetLock( secondName );

        var firstHolder = new LockHolder( firstLock, TimeSpan.Zero );
        Assert.True( await this.WithTimeout( firstHolder.Acquired ) );

        // The second name must be acquirable while the first is held, which is the whole point of naming locks.
        var secondHolder = new LockHolder( secondLock, TimeSpan.Zero );
        Assert.True( await this.WithTimeout( secondHolder.Acquired ) );

        firstHolder.Release();
        secondHolder.Release();
        await this.WithTimeout( Task.WhenAll( firstHolder.Completed, secondHolder.Completed ) );
    }

    [Fact]
    public void ReentrantAcquisition_IsDetected()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var @lock = service.GetLock( name );

        Assert.True( @lock.TryAcquire( TimeSpan.Zero, out var handle ) );
        Assert.NotNull( handle );

        try
        {
#if DEBUG
            Assert.Throws<InvalidOperationException>( () => @lock.TryAcquire( TimeSpan.Zero, out _ ) );
#else
            // A release build only reports, so that a defect that reached production behaves as it did before the
            // check existed. The underlying mutex is reentrant, so the acquisition succeeds.
            if ( @lock.TryAcquire( TimeSpan.Zero, out var reentrantHandle ) )
            {
                reentrantHandle.Dispose();
            }
#endif
        }
        finally
        {
            handle!.Dispose();
        }

        Assert.Contains( this.GetEvents(), e => e.Kind == LockEventKind.ReentrancyDetected && e.Name == name );
    }

    [Fact]
    public void ReentrancyThroughASecondLockObjectOfTheSameName_IsDetected()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var firstLock = service.GetLock( name );
        using var secondLock = service.GetLock( name );

        Assert.True( firstLock.TryAcquire( TimeSpan.Zero, out var handle ) );
        Assert.NotNull( handle );

        try
        {
#if DEBUG
            // The check is keyed on the name and not on the object, because acquiring the same name through two
            // objects deadlocks just as surely as through one.
            Assert.Throws<InvalidOperationException>( () => secondLock.TryAcquire( TimeSpan.Zero, out _ ) );
#else
            if ( secondLock.TryAcquire( TimeSpan.Zero, out var reentrantHandle ) )
            {
                reentrantHandle.Dispose();
            }
#endif
        }
        finally
        {
            handle!.Dispose();
        }

        Assert.Contains( this.GetEvents(), e => e.Kind == LockEventKind.ReentrancyDetected && e.Name == name );
    }

    [Fact]
    public void DisposingTheHandleTwice_ReleasesOnce()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var @lock = service.GetLock( name );

        Assert.True( @lock.TryAcquire( TimeSpan.Zero, out var handle ) );
        Assert.NotNull( handle );

        handle!.Dispose();

        // A second disposal must not release a lock that another thread may since have acquired, and must not
        // throw the ApplicationException that Mutex.ReleaseMutex raises when the caller is not the owner.
        handle.Dispose();

        Assert.Single( this.GetEvents(), e => e.Kind == LockEventKind.Released && e.Name == name );
    }

    [Fact]
    public async Task WhenNamedObjectsAreUnavailable_TheLockStillExcludesTheThreadsOfTheProcess()
    {
        var name = CreateName();
        var service = this.CreateService();

        // Simulate the condition of issue 272, where the operating system cannot provide named objects at all.
        service.ForceProcessLocalLocks();

        using var firstLock = service.GetLock( name );
        using var secondLock = service.GetLock( name );

        // No operating system object was opened, so nothing was created.
        Assert.DoesNotContain( this.GetEvents(), e => e.Kind == LockEventKind.Created && e.Name == name );

        var firstHolder = new LockHolder( firstLock, TimeSpan.Zero );
        Assert.True( await this.WithTimeout( firstHolder.Acquired ) );

        var blocked = this.WaitForEventAsync( LockEventKind.Blocked, name );
        var secondHolder = new LockHolder( secondLock, Timeout.InfiniteTimeSpan );

        await blocked;

        Assert.False( secondHolder.Acquired.IsCompleted );

        firstHolder.Release();

        Assert.True( await this.WithTimeout( secondHolder.Acquired ) );

        secondHolder.Release();
        await this.WithTimeout( Task.WhenAll( firstHolder.Completed, secondHolder.Completed ) );
    }

    [Fact]
    public async Task TwoServices_ShareTheSameOperatingSystemObject()
    {
        var name = CreateName();
        var firstService = this.CreateService();
        var secondService = this.CreateService();

        using var firstLock = firstService.GetLock( name );
        using var secondLock = secondService.GetLock( name );

        var firstHolder = new LockHolder( firstLock, TimeSpan.Zero );
        Assert.True( await this.WithTimeout( firstHolder.Acquired ) );

        // Two services are two views of the same machine, exactly as two processes are, so the second one must
        // not be able to acquire what the first one holds.
        var secondHolder = new LockHolder( secondLock, TimeSpan.Zero );
        Assert.False( await this.WithTimeout( secondHolder.Acquired ) );

        firstHolder.Release();
        await this.WithTimeout( firstHolder.Completed );
    }

    [Fact]
    public async Task AContenderPinnedBeforeWaiting_AcquiresOnlyOnceReleased()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var ownerLock = service.GetLock( name );
        using var contenderLock = service.GetLock( name );

        var owner = new LockHolder( ownerLock, TimeSpan.Zero );
        Assert.True( await this.WithTimeout( owner.Acquired ) );

        var beforeWait = NamedLockService.GetSyncPointName( NamedLockService.BeforeWaitLocation, name );
        this._syncProvider.EnableSyncPoint( beforeWait );

        var contender = new LockHolder( contenderLock, Timeout.InfiniteTimeSpan );

        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( beforeWait, this._timeout.Token ) );

        // The contender has decided to wait but has not waited yet. Freeing the lock now must therefore not be
        // enough for it to acquire, which is what proves that the point pins the thread rather than merely
        // observing it.
        owner.Release();
        await this.WithTimeout( owner.Completed );

        Assert.False( contender.Acquired.IsCompleted );

        this._syncProvider.ReleaseSyncPoint( beforeWait );

        Assert.True( await this.WithTimeout( contender.Acquired ) );

        contender.Release();
        await this.WithTimeout( contender.Completed );
    }

    [Fact]
    public async Task WhileAnOwnerIsPinnedAfterAcquiring_TheLockIsAlreadyEnforced()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var ownerLock = service.GetLock( name );
        using var contenderLock = service.GetLock( name );

        var afterWait = NamedLockService.GetSyncPointName( NamedLockService.AfterWaitLocation, name );
        this._syncProvider.EnableSyncPoint( afterWait );

        var owner = new LockHolder( ownerLock, TimeSpan.Zero );

        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( afterWait, this._timeout.Token ) );

        // The owner holds the lock but has not yet been given anything with which to release it. There must be no
        // window in which the lock is owned and not enforced, so a contender must fail.
        var contender = new LockHolder( contenderLock, TimeSpan.Zero );
        Assert.False( await this.WithTimeout( contender.Acquired ) );

        this._syncProvider.ReleaseSyncPoint( afterWait );

        Assert.True( await this.WithTimeout( owner.Acquired ) );

        owner.Release();
        await this.WithTimeout( owner.Completed );
    }

    [Fact]
    public async Task WhileAnOwnerIsPinnedBeforeReleasing_TheLockIsStillHeld()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var ownerLock = service.GetLock( name );
        using var contenderLock = service.GetLock( name );

        var owner = new LockHolder( ownerLock, TimeSpan.Zero );
        Assert.True( await this.WithTimeout( owner.Acquired ) );

        var beforeRelease = NamedLockService.GetSyncPointName( NamedLockService.BeforeReleaseLocation, name );
        this._syncProvider.EnableSyncPoint( beforeRelease );

        owner.Release();

        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( beforeRelease, this._timeout.Token ) );

        // The release has begun but has not completed, so the lock must still be held. Asserting on this boundary
        // is not possible without a synchronization point, because the release is otherwise instantaneous.
        var earlyContender = new LockHolder( contenderLock, TimeSpan.Zero );
        Assert.False( await this.WithTimeout( earlyContender.Acquired ) );

        // Disabling rather than releasing, because every subsequent release of this lock would otherwise be
        // pinned in its turn, and the test does not know how many of those there will be.
        this._syncProvider.DisableSyncPoint( beforeRelease );
        await this.WithTimeout( owner.Completed );

        var lateContender = new LockHolder( contenderLock, TimeSpan.Zero );
        Assert.True( await this.WithTimeout( lateContender.Acquired ) );

        lateContender.Release();
        await this.WithTimeout( lateContender.Completed );
    }

    [Fact]
    public void AnAlreadyCancelledToken_ThrowsBeforeAnythingIsAttempted()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var @lock = service.GetLock( name );
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>( () => @lock.TryAcquire( Timeout.InfiniteTimeSpan, out _, cancellation.Token ) );

        // Nothing was acquired, so the lock is free and the reentrancy bookkeeping is clean.
        Assert.True( @lock.TryAcquire( TimeSpan.Zero, out var handle ) );
        Assert.NotNull( handle );
        handle!.Dispose();
    }

    [Fact]
    public void Acquire_ReturnsAReleaserWhenTheLockIsFree()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var @lock = service.GetLock( name );

        using ( @lock.Acquire() )
        {
            Assert.Contains( this.GetEvents(), e => e.Kind == LockEventKind.Acquired && e.Name == name );
        }

        Assert.Contains( this.GetEvents(), e => e.Kind == LockEventKind.Released && e.Name == name );
    }

    [Fact]
    public async Task Acquire_ThrowsWhenTheTimeoutElapses()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var ownerLock = service.GetLock( name );
        using var contenderLock = service.GetLock( name );

        var owner = new LockHolder( ownerLock, TimeSpan.Zero );
        Assert.True( await this.WithTimeout( owner.Acquired ) );

        // A timeout of zero makes this deterministic: the lock is owned, so the acquisition cannot succeed, and
        // the test never waits.
        await this.WithTimeout(
            RunOnDedicatedThreadAsync( () => Assert.Throws<TimeoutException>( () => contenderLock.Acquire( TimeSpan.Zero ) ) ) );

        owner.Release();
        await this.WithTimeout( owner.Completed );
    }

    [Fact]
    public async Task CancellingAContenderPinnedBeforeWaiting_ThrowsAndDoesNotAcquire()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var ownerLock = service.GetLock( name );
        using var contenderLock = service.GetLock( name );
        using var cancellation = new CancellationTokenSource();

        var owner = new LockHolder( ownerLock, TimeSpan.Zero );
        Assert.True( await this.WithTimeout( owner.Acquired ) );

        var beforeWait = NamedLockService.GetSyncPointName( NamedLockService.BeforeWaitLocation, name );
        this._syncProvider.EnableSyncPoint( beforeWait );

        var contender = new LockHolder( contenderLock, Timeout.InfiniteTimeSpan, cancellation.Token );

        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( beforeWait, this._timeout.Token ) );

        // Cancelling while the contender is pinned, before it has begun to wait. Releasing the point afterwards
        // makes the outcome independent of when the thread actually reaches the wait.
        cancellation.Cancel();
        this._syncProvider.ReleaseSyncPoint( beforeWait );

        await Assert.ThrowsAsync<OperationCanceledException>( () => this.WithTimeout( contender.Acquired ) );

        // The owner still holds the lock, so the cancelled contender cannot have taken it.
        owner.Release();
        await this.WithTimeout( owner.Completed );
    }

    [Fact]
    public async Task CancellingAContenderThatIsBlocked_ThrowsAndDoesNotAcquire()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var ownerLock = service.GetLock( name );
        using var contenderLock = service.GetLock( name );
        using var cancellation = new CancellationTokenSource();

        var owner = new LockHolder( ownerLock, TimeSpan.Zero );
        Assert.True( await this.WithTimeout( owner.Acquired ) );

        var beforeWait = NamedLockService.GetSyncPointName( NamedLockService.BeforeWaitLocation, name );
        this._syncProvider.EnableSyncPoint( beforeWait );

        var contender = new LockHolder( contenderLock, Timeout.InfiniteTimeSpan, cancellation.Token );

        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( beforeWait, this._timeout.Token ) );

        // Releasing first and cancelling afterwards, so that the contender is genuinely inside the wait. This is
        // the case that exercises waking the wait through the handle of the token, rather than finding the token
        // already signalled.
        this._syncProvider.ReleaseSyncPoint( beforeWait );
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>( () => this.WithTimeout( contender.Acquired ) );

        owner.Release();
        await this.WithTimeout( owner.Completed );
    }

    [Fact]
    public async Task CancellingAnOwnerPinnedAfterAcquiring_StillYieldsAUsableLock()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var @lock = service.GetLock( name );
        using var contenderLock = service.GetLock( name );
        using var cancellation = new CancellationTokenSource();

        var afterWait = NamedLockService.GetSyncPointName( NamedLockService.AfterWaitLocation, name );
        this._syncProvider.EnableSyncPoint( afterWait );

        var owner = new LockHolder( @lock, TimeSpan.Zero, cancellation.Token );

        await this.WithTimeout( this._syncProvider.WaitForSyncPointReachedAsync( afterWait, this._timeout.Token ) );

        // The lock is owned at this point. Cancelling must not abort the acquisition, because the caller has not
        // yet been given anything with which to release it, and the lock would be leaked for the lifetime of the
        // process.
        cancellation.Cancel();

        // Disabling rather than releasing, because the contender acquired at the end of this test would otherwise
        // be pinned at the same point in its turn.
        this._syncProvider.DisableSyncPoint( afterWait );

        Assert.True( await this.WithTimeout( owner.Acquired ) );

        owner.Release();
        await this.WithTimeout( owner.Completed );

        // The release happened, so the lock is free again.
        var contender = new LockHolder( contenderLock, TimeSpan.Zero );
        Assert.True( await this.WithTimeout( contender.Acquired ) );

        contender.Release();
        await this.WithTimeout( contender.Completed );
    }

    [Fact]
    public async Task AfterACancelledAcquisition_TheSameThreadCanAcquireAgain()
    {
        var name = CreateName();
        var service = this.CreateService();

        using var @lock = service.GetLock( name );
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // A cancelled acquisition must not leave the name recorded as held by this thread, otherwise the next
        // acquisition on the same thread would be rejected as reentrant.
        await this.WithTimeout(
            RunOnDedicatedThreadAsync(
                () =>
                {
                    Assert.Throws<OperationCanceledException>( () => @lock.TryAcquire( Timeout.InfiniteTimeSpan, out _, cancellation.Token ) );

                    Assert.True( @lock.TryAcquire( TimeSpan.Zero, out var handle ) );

                    Assert.NotNull( handle );
                    handle!.Dispose();
                } ) );

        Assert.DoesNotContain( this.GetEvents(), e => e.Kind == LockEventKind.ReentrancyDetected && e.Name == name );
    }
}
