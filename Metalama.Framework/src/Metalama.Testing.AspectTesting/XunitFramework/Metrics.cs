// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading;

namespace Metalama.Testing.AspectTesting.XunitFramework;

/// <summary>
/// Counts the results of the tests of a node of the test tree (a test, a class, a collection or the assembly), and raises
/// the <see cref="Started"/> and <see cref="Finished"/> events of the node.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="Metrics"/> object except the one of the assembly has a parent. A test is reported to the object of the
/// test only, and the object propagates the report to its ancestors. The executor reports every test with
/// <see cref="OnTestsDiscovered"/> before it starts any test, so that a node cannot finish while one of its tests has not
/// been reported yet.
/// </para>
/// <para>
/// The counters of the node and of its ancestors are updated before any <see cref="Finished"/> event is raised, so that
/// the handler of the event reads the final counts. <see cref="Finished"/> is raised on the node before its parent, and
/// <see cref="Started"/> on the parent before the node, which is the order of the messages that xunit expects.
/// </para>
/// </remarks>
internal sealed class Metrics
{
    /// <summary>
    /// The metrics of the parent node, or <c>null</c> for the root node.
    /// </summary>
    private readonly Metrics? _parent;
    /// <summary>
    /// The lock under which the events of the whole tree are raised.
    /// </summary>
    private readonly object _eventLock;

    /// <summary>
    /// The number of tests that passed.
    /// </summary>
    private int _testsRun;
    /// <summary>
    /// The number of tests that failed.
    /// </summary>
    private int _testFailed;
    /// <summary>
    /// The number of tests that were skipped.
    /// </summary>
    private int _testSkipped;
    /// <summary>
    /// The number of tests that were not run.
    /// </summary>
    private int _testsNotRun;
    /// <summary>
    /// The number of reported tests that have not finished.
    /// </summary>
    private int _testsRemaining;
    /// <summary>
    /// The number of tests that have started.
    /// </summary>
    private int _testsStarted;
    /// <summary>
    /// The total execution time of the tests, in milliseconds.
    /// </summary>
    private long _executionTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="Metrics"/> class for a node that has a parent.
    /// </summary>
    public Metrics( Metrics parent )
    {
        this._parent = parent;
        this._eventLock = parent._eventLock;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Metrics"/> class for the root node.
    /// </summary>
    /// <param name="eventLock">The lock under which the events of the whole tree are raised.</param>
    public Metrics( object eventLock )
    {
        this._eventLock = eventLock;
    }

    /// <summary>
    /// Gets the number of tests that passed.
    /// </summary>
    public int TestsRun => this._testsRun;

    /// <summary>
    /// Gets the number of tests that failed.
    /// </summary>
    public int TestFailed => this._testFailed;

    /// <summary>
    /// Gets the number of tests that were skipped.
    /// </summary>
    public int TestSkipped => this._testSkipped;

    /// <summary>
    /// Gets the number of tests that were not run because the run was cancelled.
    /// </summary>
    public int TestsNotRun => this._testsNotRun;

    /// <summary>
    /// Gets the number of tests that finished, whether they passed, failed, were skipped or were not run.
    /// </summary>
    public int TestsTotal => this._testsRun + this._testFailed + this._testSkipped + this._testsNotRun;

    /// <summary>
    /// Gets the total execution time of the tests that passed or failed, in seconds.
    /// </summary>
    public decimal ExecutionTime => (decimal) TimeSpan.FromMilliseconds( Interlocked.Read( ref this._executionTime ) ).TotalSeconds;

    /// <summary>
    /// Occurs when the first test of the node starts.
    /// </summary>
    public event Action? Started;

    /// <summary>
    /// Occurs when the last test of the node finishes, provided that a test of the node has started.
    /// </summary>
    public event Action? Finished;

    /// <summary>
    /// Adds tests to the number of tests that the node and its ancestors must wait for before they finish.
    /// </summary>
    public void OnTestsDiscovered( int count )
    {
        Interlocked.Add( ref this._testsRemaining, count );
        this._parent?.OnTestsDiscovered( count );
    }

    /// <summary>
    /// Reports that a test of the node has started.
    /// </summary>
    public void OnTestStarted()
    {
        // The parent is started first, so that the starting message of the parent precedes the one of the node.
        this._parent?.OnTestStarted();

        if ( Interlocked.Increment( ref this._testsStarted ) == 1 )
        {
            lock ( this._eventLock )
            {
                this.Started?.Invoke();
            }
        }
    }

    /// <summary>
    /// Reports that a test of the node passed.
    /// </summary>
    public void OnTestSucceeded( TimeSpan duration )
    {
        for ( var node = this; node != null; node = node._parent )
        {
            Interlocked.Increment( ref node._testsRun );
            Interlocked.Add( ref node._executionTime, (long) duration.TotalMilliseconds );
        }

        this.OnTestFinished();
    }

    /// <summary>
    /// Reports that a test of the node failed.
    /// </summary>
    public void OnTestFailed( TimeSpan duration )
    {
        for ( var node = this; node != null; node = node._parent )
        {
            Interlocked.Increment( ref node._testFailed );
            Interlocked.Add( ref node._executionTime, (long) duration.TotalMilliseconds );
        }

        this.OnTestFinished();
    }

    /// <summary>
    /// Reports that a test of the node was skipped.
    /// </summary>
    public void OnTestSkipped()
    {
        for ( var node = this; node != null; node = node._parent )
        {
            Interlocked.Increment( ref node._testSkipped );
        }

        this.OnTestFinished();
    }

    /// <summary>
    /// Reports that a test of the node was not started because the run was cancelled.
    /// </summary>
    public void OnTestNotRun()
    {
        for ( var node = this; node != null; node = node._parent )
        {
            Interlocked.Increment( ref node._testsNotRun );
        }

        this.OnTestFinished();
    }

    /// <summary>
    /// Decrements the number of remaining tests of the node and of its ancestors, and raises the <see cref="Finished"/>
    /// event of every node that has no remaining test.
    /// </summary>
    private void OnTestFinished()
    {
        for ( var node = this; node != null; node = node._parent )
        {
            if ( Interlocked.Decrement( ref node._testsRemaining ) == 0 && Volatile.Read( ref node._testsStarted ) > 0 )
            {
                lock ( this._eventLock )
                {
                    node.Finished?.Invoke();
                }
            }
        }
    }
}
