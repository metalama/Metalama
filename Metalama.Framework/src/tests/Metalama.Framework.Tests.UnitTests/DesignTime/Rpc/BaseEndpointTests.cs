// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Utilities.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

/// <summary>
/// Tests for <see cref="BaseEndpoint.ExecuteBackgroundTask"/> and related functionality.
/// These tests verify the race condition fix where fast-completing tasks are properly tracked.
/// </summary>
public sealed partial class BaseEndpointTests : RpcUnitTestClass
{
    public BaseEndpointTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Tests that a synchronously-completing task (no await/yield) is handled correctly.
    /// This tests the race condition fix where a task that completes before TryAdd
    /// should not leave a stale entry in the tracking dictionary.
    /// The assertion is that WhenBackgroundTasksCompletedAsync completes (test would hang otherwise).
    /// </summary>
    [Fact]
    public async Task ExecuteBackgroundTask_SynchronouslyCompletingTask_HandledCorrectly()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(BaseEndpointTests)}_{Guid.NewGuid()}";

        using var endpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );

        // Execute a task that completes synchronously (no await/yield).
        // This creates a race where the task may complete before TryAdd.
        endpoint.ExecuteBackgroundTaskForTest(
            _ => Task.CompletedTask,
            "SynchronousTask" );

        // WhenBackgroundTasksCompletedAsync must complete. If the race condition bug existed,
        // a stale entry would remain in the dictionary and this would hang.
        await endpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );
    }

    /// <summary>
    /// Tests that a slow task (blocked until released) is properly tracked and
    /// WhenBackgroundTasksCompletedAsync waits for it.
    /// </summary>
    [Fact]
    public async Task ExecuteBackgroundTask_SlowCompletingTask_TrackedCorrectly()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(BaseEndpointTests)}_{Guid.NewGuid()}";

        using var endpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );

        // Use TaskCompletionSource to control when the task completes.
        var tcs = new TaskCompletionSource<bool>();
        var taskStarted = new TaskCompletionSource<bool>();

        endpoint.ExecuteBackgroundTaskForTest(
            async _ =>
            {
                taskStarted.SetResult( true );
                await tcs.Task.WithCancellation( testContext.CancellationToken );
            },
            "SlowCompletingTask" );

        // Wait for the task to start.
        await taskStarted.Task.WithCancellation( testContext.CancellationToken );

        // WhenBackgroundTasksCompletedAsync should NOT be completed yet because task is blocked.
        var waitTask = endpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );

        // Synchronously check that the wait task is not completed (task is still running).
        Assert.False( waitTask.IsCompleted, "WhenBackgroundTasksCompletedAsync should NOT be completed while task is running" );

        // Now release the task.
        tcs.SetResult( true );

        // WhenBackgroundTasksCompletedAsync should now complete.
        await waitTask.WithCancellation( testContext.CancellationToken );
    }

    /// <summary>
    /// Tests that a task with registerTask=false is NOT tracked by WhenBackgroundTasksCompletedAsync.
    /// </summary>
    [Fact]
    public async Task ExecuteBackgroundTask_RegisterTaskFalse_NotTracked()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(BaseEndpointTests)}_{Guid.NewGuid()}";

        using var endpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );

        // Use TaskCompletionSource to control when the task completes.
        var taskBlocker = new TaskCompletionSource<bool>();
        var taskStarted = new TaskCompletionSource<bool>();

        // Execute with registerTask=false
        endpoint.ExecuteBackgroundTaskForTest(
            async _ =>
            {
                taskStarted.SetResult( true );
                await taskBlocker.Task.WithCancellation( testContext.CancellationToken );
            },
            "UnregisteredTask",
            registerTask: false );

        // Wait for the task to start.
        await taskStarted.Task.WithCancellation( testContext.CancellationToken );

        // WhenBackgroundTasksCompletedAsync should complete immediately because
        // the task was not registered (even though the task is still running).
        Assert.True( endpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken ).IsCompleted );

        // Clean up - release the task.
        taskBlocker.SetResult( true );
    }

    /// <summary>
    /// Tests that a task which throws an exception is handled gracefully and removed from tracking.
    /// </summary>
    [Fact]
    public async Task ExecuteBackgroundTask_TaskThrowsException_HandledGracefully()
    {
        using var testContext = this.CreateRpcTestContext();

        var exceptionHandler = new TestExceptionHandler();

        var serviceProvider = testContext.Global.Underlying
            .WithUntypedService( typeof(IRpcExceptionHandler), exceptionHandler );

        var pipeName = $"{nameof(BaseEndpointTests)}_{Guid.NewGuid()}";

        using var endpoint = new TestServerEndpoint( serviceProvider, pipeName );

        var taskStarted = new TaskCompletionSource<bool>();

        // Execute a task that throws an exception.
        endpoint.ExecuteBackgroundTaskForTest(
            _ =>
            {
                taskStarted.SetResult( true );

                throw new InvalidOperationException( "Test exception" );
            },
            "ThrowingTask" );

        // Wait for the task to start (and throw).
        await taskStarted.Task.WithCancellation( testContext.CancellationToken );

        // WhenBackgroundTasksCompletedAsync should complete (task removed from tracking after exception).
        await endpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );

        // Verify exception was handled.
        Assert.True( exceptionHandler.ExceptionWasHandled, "Exception should have been handled" );
    }

    /// <summary>
    /// Tests that disposing the endpoint cancels running background tasks via DisposeCancellationToken.
    /// </summary>
    [Fact]
    public async Task ExecuteBackgroundTask_DisposeWhileTaskRunning_TaskCancelled()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(BaseEndpointTests)}_{Guid.NewGuid()}";

        var endpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );

        var taskStarted = new TaskCompletionSource<bool>();
        var taskCancelled = new TaskCompletionSource<bool>();

        // Execute a task that waits for cancellation.
        endpoint.ExecuteBackgroundTaskForTest(
            async ct =>
            {
                taskStarted.SetResult( true );

                try
                {
                    // Wait indefinitely until cancelled.
                    await Task.Delay( Timeout.Infinite, ct );
                }
                catch ( OperationCanceledException )
                {
                    taskCancelled.SetResult( true );

                    throw;
                }
            },
            "CancellableTask" );

        // Wait for the task to start.
        await taskStarted.Task.WithCancellation( testContext.CancellationToken );

        // Dispose the endpoint - this should cancel the task.
        endpoint.Dispose();

        // Verify the task was cancelled.
        await taskCancelled.Task.WithCancellation( testContext.CancellationToken );
    }

    /// <summary>
    /// Tests that multiple concurrent tasks are all tracked and WhenBackgroundTasksCompletedAsync waits for all.
    /// </summary>
    [Fact]
    public async Task ExecuteBackgroundTask_MultipleConcurrentTasks_AllTracked()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(BaseEndpointTests)}_{Guid.NewGuid()}";

        using var endpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );

        // Create blockers for 3 tasks.
        var blocker1 = new TaskCompletionSource<bool>();
        var blocker2 = new TaskCompletionSource<bool>();
        var blocker3 = new TaskCompletionSource<bool>();

        var started1 = new TaskCompletionSource<bool>();
        var started2 = new TaskCompletionSource<bool>();
        var started3 = new TaskCompletionSource<bool>();

        // Start 3 concurrent tasks.
        endpoint.ExecuteBackgroundTaskForTest(
            async _ =>
            {
                started1.SetResult( true );
                await blocker1.Task.WithCancellation( testContext.CancellationToken );
            },
            "Task1" );

        endpoint.ExecuteBackgroundTaskForTest(
            async _ =>
            {
                started2.SetResult( true );
                await blocker2.Task.WithCancellation( testContext.CancellationToken );
            },
            "Task2" );

        endpoint.ExecuteBackgroundTaskForTest(
            async _ =>
            {
                started3.SetResult( true );
                await blocker3.Task.WithCancellation( testContext.CancellationToken );
            },
            "Task3" );

        // Wait for all tasks to start.
        await started1.Task.WithCancellation( testContext.CancellationToken );
        await started2.Task.WithCancellation( testContext.CancellationToken );
        await started3.Task.WithCancellation( testContext.CancellationToken );

        // WhenBackgroundTasksCompletedAsync should NOT be completed (all 3 tasks running).
        var waitTask = endpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );
        Assert.False( waitTask.IsCompleted, "Should be waiting for all 3 tasks" );

        // Release task 1 - should still be waiting for tasks 2 and 3.
        blocker1.SetResult( true );
        Assert.False( waitTask.IsCompleted, "Should still be waiting for tasks 2 and 3" );

        // Release task 2 - should still be waiting for task 3.
        blocker2.SetResult( true );
        Assert.False( waitTask.IsCompleted, "Should still be waiting for task 3" );

        // Release task 3 - now should complete.
        blocker3.SetResult( true );

        await waitTask.WithCancellation( testContext.CancellationToken );
    }
}