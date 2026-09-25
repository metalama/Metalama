// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests the exception safety and the reentrancy of <c>SetItem</c> in <see cref="MemoryCachingBackend"/>.
/// </summary>
/// <remarks>
/// <para>
/// The tests of <c>SetItem</c> configure a size calculator or a serializer whose code the test supplies. The backend calls
/// this code while it stores an item. The tests check that a failure of this code keeps the dependency index consistent
/// with the values in the cache, and that this code can call the backend without causing a deadlock.
/// </para>
/// <para>
/// Each test asserts the correct behavior, so a test fails while the backend behaves incorrectly. Before the decisive
/// assertion, the concurrent test asserts that each thread reached the code supplied by the test.
/// </para>
/// </remarks>
public sealed partial class MemoryCachingBackendLifecycleTests
{
    /// <summary>
    /// The key of the item that most tests store.
    /// </summary>
    private const string _key = "key";

    /// <summary>
    /// The dependency of the value that a failed replacement must keep.
    /// </summary>
    private const string _previousDependency = "previous-dependency";

    /// <summary>
    /// The dependency of the value that a failed store or replacement does not store.
    /// </summary>
    private const string _newDependency = "new-dependency";

    /// <summary>
    /// Selects a size calculator as the code that the test supplies to the backend.
    /// </summary>
    private const string _sizeCalculatorComponent = "SizeCalculator";

    /// <summary>
    /// Selects a serializer as the code that the test supplies to the backend.
    /// </summary>
    private const string _serializerComponent = "Serializer";

    /// <summary>
    /// The value for which the code that the test supplies throws an exception.
    /// </summary>
    private const string _failingValue = "failing-value";

    /// <summary>
    /// The message of the exception that the code supplied by the test throws.
    /// </summary>
    private const string _simulatedFailureMessage = "The code supplied by the test failed as instructed.";

    /// <summary>
    /// The key that the first writer stores in the reentrancy test.
    /// </summary>
    private const string _firstKey = "first-key";

    /// <summary>
    /// The key that the second writer stores in the reentrancy test.
    /// </summary>
    private const string _secondKey = "second-key";

    /// <summary>
    /// The value that the first writer stores in the reentrancy test. The code supplied by the test recognizes it.
    /// </summary>
    private const string _firstMarker = "first-marker";

    /// <summary>
    /// The value that the second writer stores in the reentrancy test. The code supplied by the test recognizes it.
    /// </summary>
    private const string _secondMarker = "second-marker";

    /// <summary>
    /// The name of the thread of the first writer in the reentrancy test.
    /// </summary>
    private const string _firstWriterThreadName = "FirstWriter";

    /// <summary>
    /// The name of the thread of the second writer in the reentrancy test.
    /// </summary>
    private const string _secondWriterThreadName = "SecondWriter";

    /// <summary>
    /// The maximum time to wait for a thread or an event. The timeout only detects a failure, such as a deadlock.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// The output of the current test, which receives diagnostic lines.
    /// </summary>
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCachingBackendLifecycleTests"/> class.
    /// </summary>
    /// <param name="output">The output of the current test.</param>
    public MemoryCachingBackendLifecycleTests( ITestOutputHelper output )
    {
        this._output = output;
    }

    /// <summary>
    /// A <c>SetItem</c> call that replaces a value and fails in the size calculator or in the serializer must keep the
    /// dependency index consistent with the value that remains in the cache.
    /// </summary>
    /// <remarks>
    /// The key first holds a value that depends on one dependency. The replacement depends on another dependency, and the
    /// code supplied by the test throws for it. After the failure, the key must not be registered for the dependency of
    /// the value that was never stored, and the invalidation of the dependency of the value in the cache must remove that
    /// value. The test fails when <c>SetItem</c> removes the registrations of the previous value and adds the
    /// registrations of the new value before it calls the code that throws.
    /// </remarks>
    /// <param name="failingComponent">The component whose code throws: the size calculator or the serializer.</param>
    [Theory]
    [InlineData( _sizeCalculatorComponent )]
    [InlineData( _serializerComponent )]
    public void SetItem_ThrowingAfterDependencyUpdate_KeepsIndexConsistent( string failingComponent )
    {
        using var backend = CreateBackendWithForeignCode( "exception-safety", failingComponent, ThrowForFailingValue );

        backend.SetItem( _key, new CacheItem( "previous-value", [_previousDependency] ) );

        var exception = Record.Exception( () => backend.SetItem( _key, new CacheItem( _failingValue, [_newDependency] ) ) );

        Assert.True(
            IsSimulatedFailure( exception ),
            $"The replacement did not fail with the simulated failure of the code supplied by the test. Actual exception: {exception?.ToString() ?? "none"}" );

        Assert.False(
            backend.ContainsDependency( _newDependency ),
            "The failed replacement left the key registered for a dependency of the value that was never stored." );

        backend.InvalidateDependency( _previousDependency );

        Assert.True(
            backend.GetItem( _key ) is null,
            "The invalidation of a dependency of the value that remained in the cache did not remove that value." );
    }

    /// <summary>
    /// A first <c>SetItem</c> call for a key that fails in the size calculator or in the serializer must not leave the
    /// key registered for the dependencies of the value that was never stored.
    /// </summary>
    /// <remarks>
    /// A registration without a value is never removed: an invalidation of the dependency finds no value to remove, so
    /// it does not clean the registration. The test fails when <c>SetItem</c> registers the dependencies of the new value
    /// before it calls the code that throws.
    /// </remarks>
    /// <param name="failingComponent">The component whose code throws: the size calculator or the serializer.</param>
    [Theory]
    [InlineData( _sizeCalculatorComponent )]
    [InlineData( _serializerComponent )]
    public void SetItem_ThrowingOnFirstStoreOfKey_LeavesNoDependencyRegistration( string failingComponent )
    {
        using var backend = CreateBackendWithForeignCode( "exception-safety", failingComponent, ThrowForFailingValue );

        var exception = Record.Exception( () => backend.SetItem( _key, new CacheItem( _failingValue, [_newDependency] ) ) );

        Assert.True(
            IsSimulatedFailure( exception ),
            $"The store did not fail with the simulated failure of the code supplied by the test. Actual exception: {exception?.ToString() ?? "none"}" );

        Assert.True( backend.GetItem( _key ) is null, "The failed store left a value in the cache." );

        Assert.False(
            backend.ContainsDependency( _newDependency ),
            "The failed store left the key registered for a dependency although no value is stored." );
    }

    /// <summary>
    /// Two <c>SetItem</c> calls whose size calculator or serializer stores a value under the key of the other call must
    /// both complete.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both keys already hold a value, and each <c>SetItem</c> call acquires the lock of its key. The code supplied by the
    /// test waits on a barrier until both calls run it, and then calls <c>SetItem</c> for the key of the other call. The
    /// test fails with a deadlock when the backend runs this code while it holds the lock of the key: each call then waits
    /// for the lock that the other call holds.
    /// </para>
    /// <para>
    /// When a deadlock is detected, the backend is not disposed, because the two writers remain blocked inside it.
    /// </para>
    /// </remarks>
    /// <param name="reenteringComponent">The component whose code calls the backend: the size calculator or the serializer.</param>
    [Theory]
    [InlineData( _sizeCalculatorComponent )]
    [InlineData( _serializerComponent )]
    public void SetItem_WithForeignCodeReenteringTheBackend_DoesNotDeadlock( string reenteringComponent )
    {
        using var barrier = new Barrier( 2 );
        using var writersInForeignCode = new CountdownEvent( 2 );

        // The local function below is passed to the factory of the backend, so the variable must be assigned before the
        // backend exists. The local function runs only after the assignment of the backend.
        CachingBackend backend = null!;

        void ReenterBackend( object? value )
        {
            var nestedKey = value switch
            {
                _firstMarker => _secondKey,
                _secondMarker => _firstKey,
                _ => null
            };

            if ( nestedKey is null )
            {
                return;
            }

            if ( !barrier.SignalAndWait( _timeout ) )
            {
                throw new InvalidOperationException( "The other writer did not reach the code supplied by the test." );
            }

            writersInForeignCode.Signal();

            backend.SetItem( nestedKey, new CacheItem( "nested-value" ) );
        }

        backend = CreateBackendWithForeignCode( "reentrancy", reenteringComponent, ReenterBackend );

        // Each key already holds a value, so that each writer replaces a value, as in the original report of the defect.
        backend.SetItem( _firstKey, new CacheItem( "initial-value" ) );
        backend.SetItem( _secondKey, new CacheItem( "initial-value" ) );

        var bothCompleted = false;

        try
        {
            var firstWriter = ConcurrencyTestWorker.Start( _firstWriterThreadName, () => backend.SetItem( _firstKey, new CacheItem( _firstMarker ) ) );
            var secondWriter = ConcurrencyTestWorker.Start( _secondWriterThreadName, () => backend.SetItem( _secondKey, new CacheItem( _secondMarker ) ) );

            Assert.True( writersInForeignCode.Wait( _timeout ), "The two writers did not both reach the code supplied by the test." );

            var firstCompleted = firstWriter.Join( _timeout );

            // When the first writer has not completed, the failure is already detected, so the state of the second writer
            // is read without waiting.
            var secondCompleted = secondWriter.Join( firstCompleted ? _timeout : TimeSpan.Zero );

            this._output.WriteLine( $"First writer completed: {firstCompleted}. Second writer completed: {secondCompleted}." );

            bothCompleted = firstCompleted && secondCompleted;

            Assert.True(
                bothCompleted,
                "The two writers deadlocked. Each writer waits for the lock of the key that the other writer holds while it runs the code supplied by the test." );

            Assert.True( firstWriter.Exception is null, $"The first writer failed: {firstWriter.Exception}" );
            Assert.True( secondWriter.Exception is null, $"The second writer failed: {secondWriter.Exception}" );
        }
        finally
        {
            // A deadlocked writer remains blocked inside the backend, so the backend is disposed only when both writers
            // have completed.
            if ( bothCompleted )
            {
                backend.Dispose();
            }
        }
    }

    /// <summary>
    /// Creates and initializes a memory caching backend, over a memory cache of its own, whose size calculator or
    /// serializer invokes a callback with the value of each item that the backend stores.
    /// </summary>
    /// <param name="debugName">The debug name of the backend.</param>
    /// <param name="foreignComponent">
    /// <see cref="_sizeCalculatorComponent"/> to invoke the callback from the size calculator, or
    /// <see cref="_serializerComponent"/> to invoke it from the serializer.
    /// </param>
    /// <param name="onForeignCall">The callback, which receives the value of the stored item.</param>
    /// <returns>The initialized backend.</returns>
    private static CachingBackend CreateBackendWithForeignCode( string debugName, string foreignComponent, Action<object?> onForeignCall )
    {
        var configuration = foreignComponent switch
        {
            _sizeCalculatorComponent => new MemoryCachingBackendConfiguration
            {
                DebugName = debugName,
                SizeCalculator = value =>
                {
                    onForeignCall( value );

                    return 1;
                }
            },
            _serializerComponent => new MemoryCachingBackendConfiguration { DebugName = debugName, Serializer = new CallbackSerializer( onForeignCall ) },
            _ => throw new ArgumentOutOfRangeException( nameof(foreignComponent), foreignComponent, "The component is not supported by the test." )
        };

        var backend = CachingBackend.Create( b => b.Memory( configuration ) );

        backend.Initialize();

        return backend;
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> when the value is <see cref="_failingValue"/>.
    /// </summary>
    /// <param name="value">The value of the item that the backend stores.</param>
    private static void ThrowForFailingValue( object? value )
    {
        if ( value is _failingValue )
        {
            throw new InvalidOperationException( _simulatedFailureMessage );
        }
    }

    /// <summary>
    /// Determines whether an exception is the exception thrown by <see cref="ThrowForFailingValue"/>.
    /// </summary>
    /// <param name="exception">The exception, or <see langword="null"/> when no exception was thrown.</param>
    /// <returns><see langword="true"/> when the exception is the simulated failure, otherwise <see langword="false"/>.</returns>
    private static bool IsSimulatedFailure( Exception? exception ) => exception is InvalidOperationException { Message: _simulatedFailureMessage };
}
