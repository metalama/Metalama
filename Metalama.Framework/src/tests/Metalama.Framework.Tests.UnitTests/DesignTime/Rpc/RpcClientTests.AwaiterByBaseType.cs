// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Utilities.Threading;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcClientTests
{
    /// <summary>
    /// Regression test for #2322. A caller that waits for a client by a base type of the registered client, as
    /// <c>RpcServiceProviderClientEndpoint.GetApiAsync</c> does when it waits for <c>RpcClient&lt;TApi&gt;</c>, is
    /// released when the connection registers a client of a derived type. Before the fix, the connection looked up
    /// the awaiter by the exact type of the client, so the caller waited until the test context was cancelled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test waits until the server has created its pipe before the client connects. On .NET Framework,
    /// <c>NamedPipeClientStream.ConnectAsync</c> runs a synchronous retry loop on a thread pool thread until the pipe
    /// exists, and the server creates the pipe in a task that also requires a thread pool thread. When the thread pool
    /// of the test process is saturated, the server task can be delayed for longer than a short test timeout, and the
    /// test then fails although the code under test is correct.
    /// </para>
    /// <para>
    /// For the same reason, the test uses the default timeout of the test context. A shorter timeout is not reliable,
    /// because the timer that signals it also runs on the thread pool.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task GetOrWaitForClient_RequestedByBaseType_SignaledWhenDerivedClientConnects()
    {
        using var testContext = this.CreateRpcTestContext();
        var ct = testContext.CancellationToken;

        var pipeName = $"{nameof(RpcClientTests)}_{Guid.NewGuid()}";

        // The server reaches this synchronization point after it has created the pipe and before it waits for a client.
        var serverPipeCreated = $"ServerEndpoint.BeforeWaitForConnection:{pipeName}";
        testContext.SyncProvider.EnableSyncPoint( serverPipeCreated );

        using var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        await testContext.SyncProvider.WaitForSyncPointReachedAsync( serverPipeCreated, ct );

        // The server reaches the synchronization point again each time it accepts a client, so the test disables it
        // instead of releasing it once.
        testContext.SyncProvider.DisableSyncPoint( serverPipeCreated );

        using var clientEndpoint = new TestClientEndpoint( testContext.ServiceProvider, pipeName );

        // The endpoint is not connected, so the waiter registers an awaiter keyed by the base type RpcClient<ITestApi>.
        var waitTask = clientEndpoint.GetOrWaitForClientAsync<RpcClient<ITestApi>>( ct ).AsTask();
        Assert.False( waitTask.IsCompleted, "The wait must be pending before the endpoint connects." );

        // The connection registers a TestClient, which derives from RpcClient<ITestApi>.
        await clientEndpoint.ConnectAsync( ct );

        var client = await waitTask.WithCancellation( ct );

        Assert.Same( clientEndpoint.GetRequiredClient<TestClient>(), client );
    }
}
