// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

/// <summary>
/// Tests for <see cref="ClientEndpoint.ConnectAsync"/> and related functionality.
/// </summary>
public sealed partial class ClientEndpointTests : UnitTestClass
{
    public ClientEndpointTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Tests that when two threads call ConnectAsync concurrently, only one succeeds
    /// and the other waits for initialization.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_ConcurrentConnectAttempts_OnlyOneSucceeds()
    {
        using var testContext = this.CreateTestContext();

        var serviceProvider = testContext.ServiceProvider.Global
            .Underlying
            .WithUntypedService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );

        var pipeName = $"{nameof(ClientEndpointTests)}_{Guid.NewGuid()}";

        // Start the server first.
        using var serverEndpoint = new TestServerEndpoint( serviceProvider, pipeName );
        serverEndpoint.Start();

        // Create client endpoint.
        using var clientEndpoint = new TestClientEndpoint( serviceProvider, pipeName );

        // Signal when tasks are ready to start.
        var startSignal = new TaskCompletionSource<bool>();
        var task1Started = new TaskCompletionSource<bool>();
        var task2Started = new TaskCompletionSource<bool>();

        // Start two concurrent connection attempts.
        var connectTask1 = Task.Run(
            async () =>
            {
                task1Started.SetResult( true );
                await startSignal.Task.WithCancellation( testContext.CancellationToken );

                return await clientEndpoint.ConnectAsync( testContext.CancellationToken );
            } );

        var connectTask2 = Task.Run(
            async () =>
            {
                task2Started.SetResult( true );
                await startSignal.Task.WithCancellation( testContext.CancellationToken );

                return await clientEndpoint.ConnectAsync( testContext.CancellationToken );
            } );

        // Wait for both tasks to be ready.
        await task1Started.Task.WithCancellation( testContext.CancellationToken );
        await task2Started.Task.WithCancellation( testContext.CancellationToken );

        // Release both tasks simultaneously.
        startSignal.SetResult( true );

        // Wait for both to complete.
        var results = await Task.WhenAll( connectTask1, connectTask2 ).WithCancellation( testContext.CancellationToken );

        // One should return true (won the race), one false (lost).
        Assert.Equal( 1, results.Count( r => r ) );
        Assert.Equal( 1, results.Count( r => !r ) );
    }

    /// <summary>
    /// Tests that the loser in a connect race actually waits for the winner to complete initialization.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_LoserWaitsForWinner_BlocksUntilInitialized()
    {
        using var testContext = this.CreateTestContext();

        var serviceProvider = testContext.ServiceProvider.Global
            .Underlying
            .WithUntypedService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );

        var pipeName = $"{nameof(ClientEndpointTests)}_{Guid.NewGuid()}";

        // Start the server.
        using var serverEndpoint = new TestServerEndpoint( serviceProvider, pipeName );
        serverEndpoint.Start();

        // Create client endpoint.
        using var clientEndpoint = new TestClientEndpoint( serviceProvider, pipeName );

        // First connect call - this will win.
        var winnerTask = clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // Wait for winner to complete.
        var winnerResult = await winnerTask.WithCancellation( testContext.CancellationToken );
        Assert.True( winnerResult );

        // InitializedTask should be completed successfully.
        Assert.True( clientEndpoint.InitializedTask.Task.Status == TaskStatus.RanToCompletion );

        // Second connect call - this should return false immediately (not block).
        var loserTask = clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // Should complete immediately since initialization is already done.
        Assert.True( loserTask.IsCompleted );
        var loserResult = await loserTask.WithCancellation( testContext.CancellationToken );
        Assert.False( loserResult );
    }

    /// <summary>
    /// Tests that InitializedTask is set to completed on successful connection.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_Success_InitializedTaskCompleted()
    {
        using var testContext = this.CreateTestContext();

        var serviceProvider = testContext.ServiceProvider.Global
            .Underlying
            .WithUntypedService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );

        var pipeName = $"{nameof(ClientEndpointTests)}_{Guid.NewGuid()}";

        // Start the server.
        using var serverEndpoint = new TestServerEndpoint( serviceProvider, pipeName );
        serverEndpoint.Start();

        using var clientEndpoint = new TestClientEndpoint( serviceProvider, pipeName );

        // Before connecting, InitializedTask should not be completed.
        Assert.False( clientEndpoint.InitializedTask.Task.IsCompleted );

        // Connect.
        var result = await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // Should succeed.
        Assert.True( result );

        // InitializedTask should now be completed successfully.
        Assert.True( clientEndpoint.InitializedTask.Task.Status == TaskStatus.RanToCompletion );

        var initResult = await clientEndpoint.InitializedTask.Task.WithCancellation( testContext.CancellationToken );
        Assert.True( initResult );
    }

    /// <summary>
    /// Tests that when multiple callers race to connect, all losers wait and return false.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_MultipleLosers_AllWaitAndReturnFalse()
    {
        using var testContext = this.CreateTestContext();

        var serviceProvider = testContext.ServiceProvider.Global
            .Underlying
            .WithUntypedService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );

        var pipeName = $"{nameof(ClientEndpointTests)}_{Guid.NewGuid()}";

        // Start the server.
        using var serverEndpoint = new TestServerEndpoint( serviceProvider, pipeName );
        serverEndpoint.Start();

        using var clientEndpoint = new TestClientEndpoint( serviceProvider, pipeName );

        // Start 5 concurrent connection attempts.
        var startSignal = new TaskCompletionSource<bool>();
        var tasksStarted = new TaskCompletionSource<bool>[5];

        for ( var i = 0; i < 5; i++ )
        {
            tasksStarted[i] = new TaskCompletionSource<bool>();
        }

        var connectTasks = new Task<bool>[5];

        for ( var i = 0; i < 5; i++ )
        {
            var index = i;

            connectTasks[i] = Task.Run(
                async () =>
                {
                    tasksStarted[index].SetResult( true );
                    await startSignal.Task.WithCancellation( testContext.CancellationToken );

                    return await clientEndpoint.ConnectAsync( testContext.CancellationToken );
                } );
        }

        // Wait for all tasks to be ready.
        foreach ( var started in tasksStarted )
        {
            await started.Task.WithCancellation( testContext.CancellationToken );
        }

        // Release all tasks simultaneously.
        startSignal.SetResult( true );

        // Wait for all to complete.
        var results = await Task.WhenAll( connectTasks ).WithCancellation( testContext.CancellationToken );

        // Exactly one should return true (winner), rest should return false (losers).
        Assert.Equal( 1, results.Count( r => r ) );
        Assert.Equal( 4, results.Count( r => !r ) );
    }
}
