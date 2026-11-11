// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Testing.UnitTesting;
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
    [Fact]
    public async Task GetOrWaitForClient_RequestedByBaseType_SignaledWhenDerivedClientConnects()
    {
        // A short timeout bounds the wait when the awaiter is not signaled.
        using var testContext = this.CreateRpcTestContext( new MetalamaTestContextOptions { Timeout = TimeSpan.FromSeconds( 30 ) } );
        var ct = testContext.CancellationToken;

        var pipeName = $"{nameof(RpcClientTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

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
