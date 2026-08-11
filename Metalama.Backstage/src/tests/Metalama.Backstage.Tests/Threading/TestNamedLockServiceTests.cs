// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Testing;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Threading;

/// <summary>
/// Tests <see cref="TestNamedLockService"/>, the substitute that the other tests rely on.
/// </summary>
/// <remarks>
/// A substitute with a defect produces tests that pass for the wrong reason, so it is verified in its own right,
/// and against the same expectations as the implementation it replaces.
/// </remarks>
public sealed class TestNamedLockServiceTests : IDisposable
{
    private const string _name = "TheLock";
    private const string _otherName = "TheOtherLock";

    /// <summary>
    /// Bounds every wait, so that a defect surfaces as a failed assertion rather than as a hung test run.
    /// </summary>
    private readonly CancellationTokenSource _timeout = new( TimeSpan.FromSeconds( 30 ) );

    private readonly TestNamedLockService _locks;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestNamedLockServiceTests"/> class.
    /// </summary>
    /// <param name="logger">The xunit output helper.</param>
    public TestNamedLockServiceTests( ITestOutputHelper logger )
    {
        this._locks = new TestNamedLockService( logger.WriteLine );
    }

    /// <inheritdoc />
    public void Dispose() => this._timeout.Dispose();

    /// <summary>
    /// Runs an action on a thread of its own, because a named lock has thread affinity.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns>A task that completes when the action returns.</returns>
    private static Task RunOnDedicatedThreadAsync( Action action )
        => Task.Factory.StartNew( action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default );

    /// <summary>
    /// Awaits a task, failing rather than hanging if the watchdog elapses first.
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
                throw new TimeoutException( "The test timed out." );
            }
        }

        await task;
    }

    [Fact]
    public void AFreeLockIsAcquiredAndCounted()
    {
        using var @lock = this._locks.GetLock( _name );

        Assert.True( @lock.TryAcquire( TimeSpan.Zero, out var releaser ) );

        Assert.Contains( _name, this._locks.GetHeldLocks() );
        Assert.Contains( _name, this._locks.GetLocksHeldByCurrentThread() );

        releaser.Dispose();

        Assert.Empty( this._locks.GetHeldLocks() );
        Assert.Equal( 1, this._locks.GetAcquisitionCount( _name ) );
        Assert.Equal( 1, this._locks.GetCreationCount( _name ) );
    }

    [Fact]
    public void ANameThatWasNeverAcquiredCountsZero()
    {
        Assert.Equal( 0, this._locks.GetAcquisitionCount( _name ) );
        Assert.Empty( this._locks.GetKnownNames() );
    }

    [Fact]
    public void APinnedLockCannotBeAcquired()
    {
        using var @lock = this._locks.GetLock( _name );
        using var pin = this._locks.Pin( _name );

        // A pin stands for another process, so it belongs to no thread of this one and is not reentrancy.
        Assert.False( @lock.TryAcquire( TimeSpan.Zero, out _ ) );
    }

    [Fact]
    public async Task APinnedLockIsAcquirableOnceUnpinned()
    {
        using var @lock = this._locks.GetLock( _name );

        var pin = this._locks.Pin( _name );

        var acquired = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        var waiter = RunOnDedicatedThreadAsync(
            () =>
            {
                using var releaser = @lock.Acquire();
                acquired.TrySetResult( true );
            } );

        Assert.False( acquired.Task.IsCompleted );

        pin.Dispose();

        await this.WithTimeout( waiter );
        Assert.True( await acquired.Task );
    }

    [Fact]
    public void ReentrantAcquisitionThrows()
    {
        using var @lock = this._locks.GetLock( _name );

        Assert.True( @lock.TryAcquire( TimeSpan.Zero, out var releaser ) );

        var exception = Assert.Throws<InvalidOperationException>( () => @lock.TryAcquire( TimeSpan.Zero, out _ ) );
        Assert.Contains( "re-entrantly", exception.Message, StringComparison.Ordinal );

        releaser.Dispose();
    }

    [Fact]
    public void NestingTwoLocksThrows()
    {
        using var first = this._locks.GetLock( _name );
        using var second = this._locks.GetLock( _otherName );

        Assert.True( first.TryAcquire( TimeSpan.Zero, out var firstReleaser ) );

        // Nesting is what a deadlock is made of, so it is rejected even though nothing is holding the second one.
        var exception = Assert.Throws<InvalidOperationException>( () => second.TryAcquire( TimeSpan.Zero, out _ ) );
        Assert.Contains( "is acquired while", exception.Message, StringComparison.Ordinal );

        firstReleaser.Dispose();
    }

    [Fact]
    public void WhenNestingIsNotEnforcedItIsOnlyRecorded()
    {
        this._locks.EnforceDiscipline = false;

        using var first = this._locks.GetLock( _name );
        using var second = this._locks.GetLock( _otherName );

        Assert.True( first.TryAcquire( TimeSpan.Zero, out var firstReleaser ) );

        // Nesting is a hazard rather than a certain deadlock, so a test may knowingly accept it and the
        // acquisition proceeds.
        Assert.True( second.TryAcquire( TimeSpan.Zero, out var secondReleaser ) );

        Assert.Single( this._locks.Violations );

        secondReleaser.Dispose();
        firstReleaser.Dispose();
    }

    [Fact]
    public void ReentrancyThrowsEvenWhenNestingIsNotEnforced()
    {
        this._locks.EnforceDiscipline = false;

        using var @lock = this._locks.GetLock( _name );

        Assert.True( @lock.TryAcquire( TimeSpan.Zero, out var releaser ) );

        // Unlike nesting, a reentrant acquisition is a certain self-deadlock, so it throws whatever the test
        // asked for: recording it and waiting would simply hang.
        Assert.Throws<InvalidOperationException>( () => @lock.TryAcquire( TimeSpan.Zero, out _ ) );

        releaser.Dispose();
    }

    [Fact]
    public void ForcedTimeoutMakesTheAcquisitionFail()
    {
        using var @lock = this._locks.GetLock( _name );

        this._locks.ForceTimeout( _name );

        Assert.False( @lock.TryAcquire( TimeSpan.Zero, out _ ) );

        // Only the forced acquisitions fail: the lock itself is free.
        Assert.True( @lock.TryAcquire( TimeSpan.Zero, out var releaser ) );
        releaser.Dispose();
    }

    [Fact]
    public void ForcedTimeoutMakesAcquireThrow()
    {
        using var @lock = this._locks.GetLock( _name );

        this._locks.ForceTimeout( _name );

        Assert.Throws<TimeoutException>( () => @lock.Acquire() );
    }

    [Fact]
    public void AnArmedExceptionIsThrownOnce()
    {
        using var @lock = this._locks.GetLock( _name );

        this._locks.ArmException( _name, () => new UnauthorizedAccessException() );

        Assert.Throws<UnauthorizedAccessException>( () => @lock.TryAcquire( TimeSpan.Zero, out _ ) );

        Assert.True( @lock.TryAcquire( TimeSpan.Zero, out var releaser ) );
        releaser.Dispose();
    }

    [Fact]
    public async Task ReleasingFromAnotherThreadIsAViolation()
    {
        this._locks.EnforceDiscipline = false;

        using var @lock = this._locks.GetLock( _name );

        IDisposable? releaser = null;

        await this.WithTimeout( RunOnDedicatedThreadAsync( () => @lock.TryAcquire( TimeSpan.Zero, out releaser ) ) );

        Assert.NotNull( releaser );

        // A named lock has thread affinity, so releasing it here, on the xunit thread, is what the operating
        // system implementation would reject with an ApplicationException.
        releaser!.Dispose();

        Assert.Contains( this._locks.Violations, v => v.Contains( "thread affinity", StringComparison.Ordinal ) );
    }

    [Fact]
    public void AnAlreadyCancelledTokenThrows()
    {
        using var @lock = this._locks.GetLock( _name );
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>( () => @lock.TryAcquire( Timeout.InfiniteTimeSpan, out _, cancellation.Token ) );
    }

    [Fact]
    public async Task ADeadlockFailsInsteadOfHanging()
    {
        this._locks.EnforceDiscipline = false;

        // The nesting itself is a violation, which this test tolerates in order to reach the cycle it is about.
        using var firstOfA = this._locks.GetLock( _name );
        using var secondOfA = this._locks.GetLock( _otherName );
        using var firstOfB = this._locks.GetLock( _name );
        using var secondOfB = this._locks.GetLock( _otherName );

        var aHasFirst = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
        var bHasSecond = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        var threadA = RunOnDedicatedThreadAsync(
            () =>
            {
                firstOfA.TryAcquire( TimeSpan.Zero, out var releaser );
                aHasFirst.TrySetResult( true );
                bHasSecond.Task.GetAwaiter().GetResult();

                // Waits for the lock that thread B holds, closing the cycle. One of the two threads sees the cycle
                // and throws; the other is then released when this one gives up its lock.
                try
                {
                    secondOfA.TryAcquire( Timeout.InfiniteTimeSpan, out _ );
                }
                catch ( InvalidOperationException ) { }

                releaser?.Dispose();
            } );

        var threadB = RunOnDedicatedThreadAsync(
            () =>
            {
                secondOfB.TryAcquire( TimeSpan.Zero, out var releaser );
                bHasSecond.TrySetResult( true );
                aHasFirst.Task.GetAwaiter().GetResult();

                try
                {
                    firstOfB.TryAcquire( Timeout.InfiniteTimeSpan, out _ );
                }
                catch ( InvalidOperationException ) { }

                releaser?.Dispose();
            } );

        await this.WithTimeout( Task.WhenAll( threadA, threadB ) );

        // Both threads returned rather than deadlocking, and the cycle was reported.
        Assert.Contains( this._locks.Violations, v => v.Contains( "Deadlock", StringComparison.Ordinal ) );
    }
}
