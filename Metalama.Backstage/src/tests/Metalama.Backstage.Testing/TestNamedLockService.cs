// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Backstage.Testing;

/// <summary>
/// An implementation of <see cref="INamedLockService"/> that uses no operating system object, that reports what
/// the code under test does with its locks, and that fails the test when the code under test violates the locking
/// discipline.
/// </summary>
/// <remarks>
/// <para>
/// Because no operating system object is involved, several tests can run in parallel in the same process without
/// interfering, and no test can be disturbed by a Metalama process running on the same machine. One instance of
/// this class represents one machine: two components that share an instance are two processes of the same
/// machine, and two instances are two machines.
/// </para>
/// <para>
/// The whole state is guarded by a single monitor. Contention inside a test is irrelevant, and a single monitor
/// makes the wait-for graph, which <see cref="DetectDeadlock"/> walks, consistent by construction.
/// </para>
/// </remarks>
[PublicAPI]
public sealed partial class TestNamedLockService : INamedLockService
{
    private readonly object _sync = new();
    private readonly Dictionary<string, LockState> _locks = new( StringComparer.Ordinal );
    private readonly Dictionary<int, List<string>> _namesHeldByThread = new();
    private readonly Dictionary<int, string> _nameWaitedForByThread = new();
    private readonly List<string> _violations = new();

    /// <summary>
    /// The pending calls to <see cref="WaitForWaitersAsync"/>.
    /// </summary>
    private readonly List<(string Name, int Count, TaskCompletionSource<bool> Signal)> _waiterCountWaiters = new();

    private readonly Action<string>? _log;

    /// <summary>
    /// Gets or sets a value indicating whether acquiring a lock while holding another one throws. When
    /// <see langword="false"/>, the nesting is only recorded in <see cref="Violations"/>.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="true"/>, because nesting two named locks is the shape a deadlock is made of:
    /// another thread taking the same two in the opposite order is all it takes. A test that knowingly accepts
    /// that hazard clears this property. The two situations that this property does not govern are a reentrant
    /// acquisition and a cycle in the wait-for graph, which are certain deadlocks rather than hazards and
    /// therefore always throw.
    /// </remarks>
    public bool EnforceDiscipline { get; set; } = true;

    /// <summary>
    /// Gets or sets a timeout that overrides the one requested by the code under test, or <see langword="null"/>
    /// to honour the requested one.
    /// </summary>
    /// <remarks>
    /// The default overrides every timeout with an infinite one, because a thread held at a synchronization point
    /// while owning a lock is a normal state in these tests, and the threads waiting for it must not time out for
    /// that reason. A test that exercises the timeout uses <see cref="ForceTimeout"/>, which is deterministic.
    /// A timeout of <see cref="TimeSpan.Zero"/> is never overridden, because it expresses a request not to wait
    /// rather than a duration.
    /// </remarks>
    public TimeSpan? TimeoutOverride { get; set; } = Timeout.InfiniteTimeSpan;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestNamedLockService"/> class.
    /// </summary>
    /// <param name="log">
    /// An optional delegate receiving trace messages, typically the <c>WriteLine</c> method of a test output
    /// helper. Passing the same delegate as the one given to the synchronization provider interleaves the two
    /// traces, which is what makes a failure readable.
    /// </param>
    public TestNamedLockService( Action<string>? log = null )
    {
        this._log = log;
    }

    /// <inheritdoc />
    public INamedLock GetLock( string name, CancellationToken cancellationToken = default )
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock ( this._sync )
        {
            this.GetOrCreateState( name ).CreationCount++;
        }

        this.Log( $"GetLock '{name}'." );

        return new Lock( this, name );
    }

    /// <summary>
    /// Gets the descriptions of the locking discipline violations observed so far. It is empty unless
    /// <see cref="EnforceDiscipline"/> is <see langword="false"/>, because a violation otherwise throws.
    /// </summary>
    public IReadOnlyList<string> Violations
    {
        get
        {
            lock ( this._sync )
            {
                return this._violations.ToList();
            }
        }
    }

    /// <summary>
    /// Gets the number of times a lock of a given name has been acquired.
    /// </summary>
    /// <param name="name">The name of the lock.</param>
    /// <returns>The number of successful acquisitions.</returns>
    /// <remarks>
    /// This is what a test asserts on to prove that an operation takes a lock once rather than several times, or
    /// that a read takes none at all.
    /// </remarks>
    public int GetAcquisitionCount( string name )
    {
        lock ( this._sync )
        {
            return this._locks.TryGetValue( name, out var state ) ? state.AcquisitionCount : 0;
        }
    }

    /// <summary>
    /// Gets the number of times a lock of a given name has been created by <see cref="GetLock"/>.
    /// </summary>
    /// <param name="name">The name of the lock.</param>
    /// <returns>The number of times the lock was created.</returns>
    public int GetCreationCount( string name )
    {
        lock ( this._sync )
        {
            return this._locks.TryGetValue( name, out var state ) ? state.CreationCount : 0;
        }
    }

    /// <summary>
    /// Gets the names of the locks that are held at this moment, by any thread.
    /// </summary>
    /// <returns>The names, in no particular order.</returns>
    public IReadOnlyList<string> GetHeldLocks()
    {
        lock ( this._sync )
        {
            return this._locks.Where( p => p.Value.OwnerThreadId != null ).Select( p => p.Key ).ToList();
        }
    }

    /// <summary>
    /// Gets the names of the locks held by the calling thread.
    /// </summary>
    /// <returns>The names, from the first acquired to the last.</returns>
    /// <remarks>
    /// A callback of the code under test calls this to assert that it does not run while a lock is held, which is
    /// the property that keeps an event handler from taking a second lock and deadlocking.
    /// </remarks>
    public IReadOnlyList<string> GetLocksHeldByCurrentThread()
    {
        lock ( this._sync )
        {
            return this._namesHeldByThread.TryGetValue( Environment.CurrentManagedThreadId, out var held ) ? held.ToList() : new List<string>();
        }
    }

    /// <summary>
    /// Gets the names of every lock this service has been asked for.
    /// </summary>
    /// <returns>The names, in no particular order.</returns>
    public IReadOnlyList<string> GetKnownNames()
    {
        lock ( this._sync )
        {
            return this._locks.Keys.ToList();
        }
    }

    /// <summary>
    /// Returns a task that completes once at least a given number of threads are blocked waiting for a lock of a
    /// given name.
    /// </summary>
    /// <param name="name">The name of the lock.</param>
    /// <param name="count">The number of waiting threads to wait for.</param>
    /// <param name="cancellationToken">A token that abandons the wait.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    /// <remarks>
    /// A test that has driven one thread into the critical section uses this to establish that a second thread is
    /// genuinely waiting for the lock, rather than assuming it after a delay. Without it, releasing the first
    /// thread could easily happen before the second one has even asked for the lock, which is a different
    /// interleaving from the one the test means to exercise.
    /// </remarks>
    public async Task WaitForWaitersAsync( string name, int count, CancellationToken cancellationToken = default )
    {
        TaskCompletionSource<bool> signal;

        lock ( this._sync )
        {
            if ( this.CountWaitersWithinLock( name ) >= count )
            {
                return;
            }

            signal = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
            this._waiterCountWaiters.Add( (name, count, signal) );
        }

        this.Log( $"WaitForWaitersAsync '{name}' ({count}): waiting." );

        using ( cancellationToken.Register( () => signal.TrySetCanceled( cancellationToken ) ) )
        {
            await signal.Task;
        }

        this.Log( $"WaitForWaitersAsync '{name}' ({count}): reached." );
    }

    /// <summary>
    /// Counts the threads currently blocked waiting for a lock of a given name.
    /// </summary>
    /// <param name="name">The name of the lock.</param>
    /// <returns>The number of waiting threads.</returns>
    /// <remarks>Must be called while <see cref="_sync"/> is held.</remarks>
    private int CountWaitersWithinLock( string name )
    {
        var count = 0;

        foreach ( var waitedFor in this._nameWaitedForByThread.Values )
        {
            if ( string.Equals( waitedFor, name, StringComparison.Ordinal ) )
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Releases the tasks of <see cref="WaitForWaitersAsync"/> whose condition has become true.
    /// </summary>
    /// <param name="name">The name a thread has just started waiting for.</param>
    /// <remarks>Must be called while <see cref="_sync"/> is held.</remarks>
    private void SignalWaiterCountWithinLock( string name )
    {
        if ( this._waiterCountWaiters.Count == 0 )
        {
            return;
        }

        var count = this.CountWaitersWithinLock( name );

        for ( var i = this._waiterCountWaiters.Count - 1; i >= 0; i-- )
        {
            var waiter = this._waiterCountWaiters[i];

            if ( string.Equals( waiter.Name, name, StringComparison.Ordinal ) && count >= waiter.Count )
            {
                this._waiterCountWaiters.RemoveAt( i );
                waiter.Signal.TrySetResult( true );
            }
        }
    }

    /// <summary>
    /// Holds a lock, as another process would, until the returned object is disposed.
    /// </summary>
    /// <param name="name">The name of the lock to hold.</param>
    /// <returns>An object that releases the lock when it is disposed.</returns>
    /// <remarks>
    /// The lock is held by no thread of this process, so the code under test blocks on it exactly as it would on
    /// a lock held by another process, and the reentrancy check does not object.
    /// </remarks>
    public IDisposable Pin( string name )
    {
        lock ( this._sync )
        {
            var state = this.GetOrCreateState( name );

            while ( state.OwnerThreadId != null || state.IsPinned )
            {
                Monitor.Wait( this._sync );
            }

            state.IsPinned = true;
        }

        this.Log( $"Pin '{name}'." );

        return new PinHandle( this, name );
    }

    /// <summary>
    /// Makes the next acquisition of a given name fail as if the timeout had elapsed.
    /// </summary>
    /// <param name="name">The name of the lock.</param>
    /// <param name="count">The number of acquisitions to fail.</param>
    public void ForceTimeout( string name, int count = 1 )
    {
        lock ( this._sync )
        {
            this.GetOrCreateState( name ).ForcedTimeouts += count;
        }

        this.Log( $"ForceTimeout '{name}' ({count})." );
    }

    /// <summary>
    /// Makes the next acquisition of a given name report that the previous owner terminated without releasing the
    /// lock.
    /// </summary>
    /// <param name="name">The name of the lock.</param>
    public void Abandon( string name )
    {
        lock ( this._sync )
        {
            this.GetOrCreateState( name ).IsAbandoned = true;
        }

        this.Log( $"Abandon '{name}'." );
    }

    /// <summary>
    /// Makes the next acquisition of a given name throw, so that a test can verify how the code under test
    /// handles a failure of the operating system.
    /// </summary>
    /// <param name="name">The name of the lock.</param>
    /// <param name="exceptionFactory">Creates the exception to throw.</param>
    public void ArmException( string name, Func<Exception> exceptionFactory )
    {
        lock ( this._sync )
        {
            this.GetOrCreateState( name ).ArmedException = exceptionFactory;
        }

        this.Log( $"ArmException '{name}'." );
    }

    /// <summary>
    /// Gets the state of a lock, creating it if this is the first time the name is seen.
    /// </summary>
    /// <param name="name">The name of the lock.</param>
    /// <returns>The state.</returns>
    /// <remarks>Must be called while <see cref="_sync"/> is held.</remarks>
    private LockState GetOrCreateState( string name )
    {
        if ( !this._locks.TryGetValue( name, out var state ) )
        {
            state = new LockState();
            this._locks.Add( name, state );
        }

        return state;
    }

    /// <summary>
    /// Writes a trace message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void Log( string message ) => this._log?.Invoke( $"TestNamedLockService: {message}" );

    /// <summary>
    /// Records a violation of the locking discipline, and throws if <see cref="EnforceDiscipline"/> is set.
    /// </summary>
    /// <param name="message">The description of the violation.</param>
    /// <remarks>Must be called while <see cref="_sync"/> is held.</remarks>
    private void Fail( string message )
    {
        this._violations.Add( message );
        this.Log( $"VIOLATION: {message}" );

        if ( this.EnforceDiscipline )
        {
            throw new InvalidOperationException( message );
        }
    }

    /// <summary>
    /// Verifies that acquiring a given name on the calling thread does not break the locking discipline.
    /// </summary>
    /// <param name="name">The name about to be acquired.</param>
    /// <remarks>
    /// <para>
    /// Two things are rejected. Acquiring a name the thread already holds is rejected because the locks are not
    /// reentrant, so it deadlocks against an implementation backed by anything other than a mutex. Acquiring a
    /// second name while holding a first one is rejected because it is the shape a deadlock is made of: another
    /// thread taking the two in the opposite order is all it takes.
    /// </para>
    /// <para>Must be called while <see cref="_sync"/> is held.</para>
    /// </remarks>
    private void VerifyDiscipline( string name )
    {
        if ( !this._namesHeldByThread.TryGetValue( Environment.CurrentManagedThreadId, out var held ) || held.Count == 0 )
        {
            return;
        }

        if ( held.Contains( name, StringComparer.Ordinal ) )
        {
            var message =
                $"The lock '{name}' is acquired re-entrantly. Named locks are not reentrant. Locks already held by this thread: {string.Join( ", ", held )}.";

            this._violations.Add( message );
            this.Log( $"VIOLATION: {message}" );

            // Always throws, whatever EnforceDiscipline says, because this is not a hazard that a test can
            // knowingly accept: the lock is held and not reentrant, so the acquisition can only wait for a lock
            // that the waiting thread itself owns. Recording and continuing would simply hang.
            throw new InvalidOperationException( message );
        }

        this.Fail(
            $"The lock '{name}' is acquired while '{held[held.Count - 1]}' is held. Nesting named locks deadlocks as soon as another "
            + $"thread takes them in the opposite order. Locks already held by this thread: {string.Join( ", ", held )}." );
    }

    /// <summary>
    /// Throws when the calling thread, by waiting for a given name, would close a cycle in the wait-for graph, so
    /// that a deadlock fails the test at once instead of waiting out a timeout.
    /// </summary>
    /// <param name="name">The name the calling thread is about to wait for.</param>
    /// <remarks>Must be called while <see cref="_sync"/> is held.</remarks>
    private void DetectDeadlock( string name )
    {
        var startThreadId = Environment.CurrentManagedThreadId;
        var currentName = name;
        var chain = new StringBuilder();
        var visited = new HashSet<int>();

        while ( true )
        {
            if ( !this._locks.TryGetValue( currentName, out var state ) || state.OwnerThreadId == null )
            {
                return;
            }

            var ownerThreadId = state.OwnerThreadId.Value;
            // string.Format, and not an interpolated string, because the overload of StringBuilder.Append that
            // takes a format provider does not exist in .NET Framework.
            chain.Append( string.Format( CultureInfo.InvariantCulture, " -> '{0}' held by thread {1}", currentName, ownerThreadId ) );

            if ( ownerThreadId == startThreadId )
            {
                var message = $"Deadlock: thread {startThreadId} waits for a chain that returns to itself:{chain}.";

                this._violations.Add( message );
                this.Log( $"VIOLATION: {message}" );

                // Always throws, even when EnforceDiscipline is not set, because the alternative is that both
                // threads wait forever: recording a violation nobody will ever read is not an option here.
                throw new InvalidOperationException( message );
            }

            if ( !visited.Add( ownerThreadId ) || !this._nameWaitedForByThread.TryGetValue( ownerThreadId, out var nextName ) )
            {
                return;
            }

            currentName = nextName;
        }
    }
}
