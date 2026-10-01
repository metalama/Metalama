// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests the initialization, the disposal and the events of <see cref="CachingBackend"/> when they run concurrently, on
/// instances of <see cref="MemoryCachingBackend"/>.
/// </summary>
/// <remarks>
/// <para>
/// The tests of the initialization and of the disposal use a <see cref="GatedMemoryCachingBackend"/>, whose
/// initialization and disposal wait at gates that the test opens. They rely on the order in which
/// <see cref="SemaphoreSlim"/> releases its asynchronous waiters, which is the order in which the waiters started to
/// wait. Each asynchronous call is started from the test method and returns a pending task, so the order of the waiters
/// is the order of the calls, and no synchronization point is needed in the product.
/// </para>
/// <para>
/// The synchronous paths are not used for these schedules. <see cref="SemaphoreSlim"/> wakes a synchronous waiter
/// through its monitor, and the test cannot observe that a thread has started to wait on the monitor.
/// </para>
/// <para>
/// The tests of the events use an <see cref="ExceptionRecordingWorkItemDispatcher"/>, which records the exception that
/// escapes a work item instead of terminating the test host.
/// </para>
/// </remarks>
public sealed partial class CachingBackendLifecycleConcurrencyTests
{
    /// <summary>
    /// The maximum time to wait for a task, a gate or a worker. It only detects a failure, such as a deadlock.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// The output of the current test, which receives diagnostic lines about the tasks that did not complete as expected.
    /// </summary>
    private readonly ITestOutputHelper _testOutput;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingBackendLifecycleConcurrencyTests"/> class.
    /// </summary>
    /// <param name="testOutput">The output of the current test.</param>
    public CachingBackendLifecycleConcurrencyTests( ITestOutputHelper testOutput )
    {
        this._testOutput = testOutput;
    }

    /// <summary>
    /// An operation that waits for the initialization behind a disposal must complete with an
    /// <see cref="ObjectDisposedException"/> when the disposal completes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The initialization holds the initialization semaphore and waits at the gate of the backend. The disposal observes
    /// the <see cref="CachingBackendStatus.Initializing"/> status and becomes the first asynchronous waiter of the
    /// semaphore. The operation calls <see cref="CachingBackend.InitializeAsync"/>, observes the same status, and
    /// becomes the second asynchronous waiter. When the test opens the gate, the initialization completes and releases
    /// the semaphore to the disposal.
    /// </para>
    /// <para>
    /// If the disposal disposes the semaphore without releasing it, the operation never acquires the semaphore and never
    /// completes. A correct disposal lets the operation acquire the semaphore, observe the disposed status, and throw an
    /// <see cref="ObjectDisposedException"/>.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task DisposeAsync_DuringInitialization_CompletesQueuedOperation()
    {
        using var timeoutSource = new CancellationTokenSource( _timeout );
        using var operationCancellationSource = new CancellationTokenSource();

        // The calls under test dispose the backend, so the backend is not declared with a using statement.
        var backend = new GatedMemoryCachingBackend();

        try
        {
            var initialization = await StartGatedInitializationAsync( backend, operationCancellationSource.Token, timeoutSource.Token );

            // The disposal observes the Initializing status and becomes the first asynchronous waiter of the
            // initialization semaphore.
            var disposal = backend.DisposeAsync( operationCancellationSource.Token ).AsTask();

            // The operation calls InitializeAsync, observes the same status, and becomes the second asynchronous waiter.
            var operation = backend.GetItemAsync( "item", cancellationToken: operationCancellationSource.Token ).AsTask();

            Assert.False( disposal.IsCompleted, "The disposal did not wait for the initialization." );
            Assert.False( operation.IsCompleted, "The operation did not wait for the initialization." );

            // The initialization completes and releases the semaphore to its first asynchronous waiter, the disposal.
            backend.OpenInitializationGate();

            await AssertCompletesSuccessfullyAsync( initialization, "The initialization", timeoutSource.Token );
            await AssertCompletesSuccessfullyAsync( disposal, "The disposal", timeoutSource.Token );

            Assert.True(
                backend.Status == CachingBackendStatus.Disposed,
                $"The status was {backend.Status} instead of Disposed after the disposal completed." );

            var operationCompleted = await WaitForCompletionAsync( operation, timeoutSource.Token );

            this._testOutput.WriteLine( $"The status of the queued operation is {operation.Status}." );

            Assert.True(
                operationCompleted,
                "The operation that waited for the initialization behind the disposal did not complete." );
            Assert.True( operation.IsFaulted, $"The operation ended with the status {operation.Status} instead of failing." );
            Assert.IsType<ObjectDisposedException>( operation.Exception?.InnerException );
        }
        finally
        {
            backend.OpenInitializationGate();
            operationCancellationSource.Cancel();
        }
    }

    /// <summary>
    /// Two disposals that start during the initialization must both complete.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The initialization holds the initialization semaphore and waits at the gate of the backend. Both disposals
    /// observe the <see cref="CachingBackendStatus.Initializing"/> status and become the first and the second
    /// asynchronous waiters of the semaphore. When the test opens the gate, the initialization completes and releases
    /// the semaphore to the first disposal.
    /// </para>
    /// <para>
    /// If the first disposal disposes the semaphore without releasing it, the second disposal never acquires the
    /// semaphore and never completes. A correct second disposal waits until the first disposal has completed, and then
    /// completes successfully, as a second disposal of an initialized backend does.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task DisposeAsync_CalledTwiceDuringInitialization_CompletesBothCalls()
    {
        using var timeoutSource = new CancellationTokenSource( _timeout );
        using var operationCancellationSource = new CancellationTokenSource();

        // The calls under test dispose the backend, so the backend is not declared with a using statement.
        var backend = new GatedMemoryCachingBackend();

        try
        {
            var initialization = await StartGatedInitializationAsync( backend, operationCancellationSource.Token, timeoutSource.Token );

            // Both disposals observe the Initializing status and wait for the initialization semaphore, in this order.
            var firstDisposal = backend.DisposeAsync( operationCancellationSource.Token ).AsTask();
            var secondDisposal = backend.DisposeAsync( operationCancellationSource.Token ).AsTask();

            Assert.False( firstDisposal.IsCompleted, "The first disposal did not wait for the initialization." );
            Assert.False( secondDisposal.IsCompleted, "The second disposal did not wait for the initialization." );

            // The initialization completes and releases the semaphore to its first asynchronous waiter, the first disposal.
            backend.OpenInitializationGate();

            await AssertCompletesSuccessfullyAsync( initialization, "The initialization", timeoutSource.Token );
            await AssertCompletesSuccessfullyAsync( firstDisposal, "The first disposal", timeoutSource.Token );

            Assert.True(
                backend.Status == CachingBackendStatus.Disposed,
                $"The status was {backend.Status} instead of Disposed after the first disposal completed." );

            var secondDisposalCompleted = await WaitForCompletionAsync( secondDisposal, timeoutSource.Token );

            this._testOutput.WriteLine( $"The status of the second disposal is {secondDisposal.Status}." );

            Assert.True(
                secondDisposalCompleted,
                "The second disposal, which waited for the initialization behind the first disposal, did not complete." );

            Assert.True(
                secondDisposal.Status == TaskStatus.RanToCompletion,
                $"The second disposal ended with the status {secondDisposal.Status} instead of completing successfully." );
        }
        finally
        {
            backend.OpenInitializationGate();
            operationCancellationSource.Cancel();
        }
    }

    /// <summary>
    /// A disposal that waits for the completion of a disposal that another thread has started must stop waiting when its
    /// cancellation token is signalled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first disposal runs on a worker thread and waits at the gate of the backend, in the
    /// <see cref="CachingBackendStatus.Disposing"/> status. The second disposal observes that status, fails its three
    /// compare-and-swap operations on the status, and waits for the completion of the first disposal. The synchronous
    /// <see cref="CachingBackend.Dispose(CancellationToken)"/> observes its cancellation token in that wait, and
    /// <see cref="CachingBackend.DisposeAsync(CancellationToken)"/> must observe its token in the same way.
    /// </para>
    /// <para>
    /// A disposal also reaches this wait when it observes the <see cref="CachingBackendStatus.Default"/> status and a
    /// concurrent initialization changes the status to <see cref="CachingBackendStatus.Initializing"/> before the
    /// compare-and-swap operations. If no thread completes the disposal in that case, the cancellation token is the only
    /// means for the caller to stop waiting. That interleaving cannot be forced without a synchronization point in the
    /// product, so this test covers the wait with a disposal that is in progress instead.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task DisposeAsync_WhileAnotherDisposalIsInProgress_ObservesCancellation()
    {
        using var timeoutSource = new CancellationTokenSource( _timeout );
        using var secondDisposalCancellationSource = new CancellationTokenSource();
        var backend = new GatedMemoryCachingBackend();
        var firstDisposal = ConcurrencyTestWorker.Start( "FirstDisposal", () => backend.Dispose() );
        bool firstDisposalJoined;

        try
        {
            Assert.True(
                await WaitForCompletionAsync( backend.DisposalReached, timeoutSource.Token ),
                "The first disposal did not reach the gate of the backend." );

            Assert.True(
                backend.Status == CachingBackendStatus.Disposing,
                $"The status was {backend.Status} instead of Disposing while the first disposal waited at the gate." );

            // The second disposal fails its compare-and-swap operations and waits for the completion of the first disposal.
            var secondDisposal = backend.DisposeAsync( secondDisposalCancellationSource.Token ).AsTask();

            Assert.False( secondDisposal.IsCompleted, "The second disposal did not wait for the first disposal." );

            secondDisposalCancellationSource.Cancel();

            var secondDisposalCompleted = await WaitForCompletionAsync( secondDisposal, timeoutSource.Token );

            this._testOutput.WriteLine( $"The status of the second disposal is {secondDisposal.Status}." );

            Assert.True(
                secondDisposalCompleted,
                "The second disposal did not observe the cancellation of its token while it waited for the first disposal." );

            Assert.True(
                secondDisposal.IsCanceled,
                $"The second disposal ended with the status {secondDisposal.Status} instead of being canceled." );
        }
        finally
        {
            backend.OpenDisposalGate();
            firstDisposalJoined = firstDisposal.Join( _timeout );
        }

        Assert.True( firstDisposalJoined, "The first disposal did not complete after the gate was opened." );
        Assert.Null( firstDisposal.Exception );

        Assert.True(
            backend.Status == CachingBackendStatus.Disposed,
            $"The status was {backend.Status} instead of Disposed after the first disposal completed." );
    }

    /// <summary>
    /// An exception that a handler of <see cref="CachingBackend.ItemRemoved"/> throws must not escape the work item that
    /// invokes the handler.
    /// </summary>
    /// <remarks>
    /// The backend invokes the handlers of an event in a work item of its <see cref="IWorkItemDispatcher"/>. The default
    /// <see cref="ThreadPoolWorkItemDispatcher"/> runs the work item on a thread-pool thread, where an exception that
    /// escapes the work item is unhandled and terminates the process. The backend must therefore handle the exceptions
    /// that the handlers throw. The test uses a dispatcher that records the exception that escapes a work item, so that
    /// the test fails instead of terminating the test host.
    /// </remarks>
    [Fact]
    public void ItemRemoved_HandlerThrows_ExceptionDoesNotEscapeWorkItem()
    {
        var dispatcher = new ExceptionRecordingWorkItemDispatcher();
        using var serviceProvider = new ServiceCollection().AddSingleton<IWorkItemDispatcher>( dispatcher ).BuildServiceProvider();
        using var backend = CreateBackend( serviceProvider );
        var invocationCount = 0;

        backend.ItemRemoved += ( _, _ ) =>
        {
            Interlocked.Increment( ref invocationCount );

            throw new InvalidOperationException( "The event handler failed." );
        };

        backend.SetItem( "item", new CacheItem( "value" ) );
        backend.RemoveItem( "item" );

        var workers = JoinWorkItems( dispatcher );

        Assert.True( Volatile.Read( ref invocationCount ) > 0, "The handler of the ItemRemoved event was not invoked." );

        this.AssertNoExceptionEscaped( workers );
    }

    /// <summary>
    /// An exception that a handler of <see cref="CachingBackend.DependencyInvalidated"/> throws must not escape the work
    /// item that invokes the handler.
    /// </summary>
    /// <remarks>
    /// The backend raises the event for every invalidated dependency, including a dependency that no item uses. The
    /// remarks of <see cref="ItemRemoved_HandlerThrows_ExceptionDoesNotEscapeWorkItem"/> describe the dispatcher that the
    /// test uses.
    /// </remarks>
    [Fact]
    public void DependencyInvalidated_HandlerThrows_ExceptionDoesNotEscapeWorkItem()
    {
        var dispatcher = new ExceptionRecordingWorkItemDispatcher();
        using var serviceProvider = new ServiceCollection().AddSingleton<IWorkItemDispatcher>( dispatcher ).BuildServiceProvider();
        using var backend = CreateBackend( serviceProvider );
        var invocationCount = 0;

        backend.DependencyInvalidated += ( _, _ ) =>
        {
            Interlocked.Increment( ref invocationCount );

            throw new InvalidOperationException( "The event handler failed." );
        };

        backend.InvalidateDependency( "dependency" );

        var workers = JoinWorkItems( dispatcher );

        Assert.True( Volatile.Read( ref invocationCount ) > 0, "The handler of the DependencyInvalidated event was not invoked." );

        this.AssertNoExceptionEscaped( workers );
    }

    /// <summary>
    /// Creates and initializes a <see cref="MemoryCachingBackend"/> that resolves its services, in particular its
    /// <see cref="IWorkItemDispatcher"/>, from the given service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider of the backend.</param>
    /// <returns>The initialized backend, which owns a new <see cref="Microsoft.Extensions.Caching.Memory.MemoryCache"/>.</returns>
    private static CachingBackend CreateBackend( IServiceProvider serviceProvider )
    {
        var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = "test" } ),
            serviceProvider );

        backend.Initialize();

        return backend;
    }

    /// <summary>
    /// Starts the asynchronous initialization of a <see cref="GatedMemoryCachingBackend"/>, and asserts that the
    /// initialization holds the initialization semaphore and waits at the gate of the backend.
    /// </summary>
    /// <param name="backend">The backend to initialize.</param>
    /// <param name="operationCancellationToken">The cancellation token passed to the initialization.</param>
    /// <param name="timeoutCancellationToken">A token signalled when the timeout of the test elapses.</param>
    /// <returns>The pending task of the initialization.</returns>
    private static async Task<Task> StartGatedInitializationAsync(
        GatedMemoryCachingBackend backend,
        CancellationToken operationCancellationToken,
        CancellationToken timeoutCancellationToken )
    {
        var initialization = backend.InitializeAsync( operationCancellationToken ).AsTask();

        Assert.True(
            await WaitForCompletionAsync( backend.InitializationReached, timeoutCancellationToken ),
            "The initialization did not reach the gate of the backend." );

        Assert.False( initialization.IsCompleted, "The initialization completed before the gate was opened." );

        Assert.True(
            backend.Status == CachingBackendStatus.Initializing,
            $"The status was {backend.Status} instead of Initializing while the initialization waited at the gate." );

        return initialization;
    }

    /// <summary>
    /// Asserts that a task completes successfully before the timeout of the test elapses.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <param name="description">The description of the task, used as the subject of the assertion messages.</param>
    /// <param name="cancellationToken">A token signalled when the timeout of the test elapses.</param>
    private static async Task AssertCompletesSuccessfullyAsync( Task task, string description, CancellationToken cancellationToken )
    {
        Assert.True( await WaitForCompletionAsync( task, cancellationToken ), $"{description} did not complete." );

        Assert.True(
            task.Status == TaskStatus.RanToCompletion,
            $"{description} ended with the status {task.Status} instead of completing successfully. Exception: {task.Exception}" );
    }

    /// <summary>
    /// Waits until the work items that a dispatcher has dispatched so far have completed.
    /// </summary>
    /// <param name="dispatcher">The dispatcher.</param>
    /// <returns>The workers that ran the work items.</returns>
    private static IReadOnlyList<ConcurrencyTestWorker> JoinWorkItems( ExceptionRecordingWorkItemDispatcher dispatcher )
    {
        var workers = dispatcher.GetWorkers();

        Assert.True( workers.Count > 0, "The backend did not dispatch any work item." );

        foreach ( var worker in workers )
        {
            Assert.True( worker.Join( _timeout ), $"The work item {worker.Name} did not complete." );
        }

        return workers;
    }

    /// <summary>
    /// Asserts that no exception escaped the work items that the given workers ran, and writes every escaped exception
    /// to the output of the test.
    /// </summary>
    /// <param name="workers">The workers, which have completed.</param>
    private void AssertNoExceptionEscaped( IReadOnlyList<ConcurrencyTestWorker> workers )
    {
        var escapedExceptionCount = 0;

        foreach ( var worker in workers )
        {
            if ( worker.Exception != null )
            {
                escapedExceptionCount++;
                this._testOutput.WriteLine( $"An exception escaped the work item {worker.Name}: {worker.Exception}" );
            }
        }

        Assert.True(
            escapedExceptionCount == 0,
            $"Exceptions thrown by an event handler escaped from work items. Escaped exceptions: {escapedExceptionCount}. "
            + $"Work items: {workers.Count}." );
    }

    /// <summary>
    /// Waits until a task completes or a cancellation token is signalled, whichever comes first.
    /// </summary>
    /// <param name="task">The task to wait for.</param>
    /// <param name="cancellationToken">A token that ends the wait.</param>
    /// <returns><see langword="true"/> when the task has completed, otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// The method does not throw when the token is signalled, so that the caller can report which task did not complete.
    /// It does not observe the outcome of the task either.
    /// </remarks>
    private static async Task<bool> WaitForCompletionAsync( Task task, CancellationToken cancellationToken )
    {
        var cancellationTaskSource = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        using ( cancellationToken.Register( () => cancellationTaskSource.TrySetResult( true ) ) )
        {
            var completedTask = await Task.WhenAny( task, cancellationTaskSource.Task );

            return completedTask == task;
        }
    }
}
