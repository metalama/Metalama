// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code

using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Testing.UnitTesting;
using System;
using System.Threading.Tasks;
using Xunit;

// ReSharper disable AccessToDisposedClosure

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcServiceProviderTests
{
    /// <summary>
    /// Regression test for #2322, which reproduces the interleaving that made
    /// <c>DesignTimeExtensionRpcTests.ExtensionRpc_MultipleProjects_IsolatedExtensions</c> time out. The server adds
    /// the service of an extension after startup, and the client starts a connection to the new pipe when it receives
    /// the <c>ServicesAdded</c> event. <c>GetApiAsync</c> is called while that connection has registered the client by
    /// interface name but has not published it yet. The call retrieves the registered services, finds the client
    /// already registered, observes that it is not published, and registers an awaiter for
    /// <c>RpcClient&lt;IExtensionService&gt;</c>, a base type of the registered client. The test asserts that the
    /// connection releases this awaiter. Before the fix, the connection looked up the awaiter by the exact type of
    /// the client, so the call waited until the test context was cancelled.
    /// </summary>
    [Fact]
    public async Task GetApiAsync_CalledWhileExtensionClientIsConnecting_Completes()
    {
        // A short timeout bounds the wait when the awaiter is not signaled.
        using var testContext = this.CreateRpcTestContext( new TestContextOptions { Timeout = TimeSpan.FromSeconds( 30 ) } );
        var ct = testContext.CancellationToken;

        // ReSharper disable once UseAwaitUsing
        using var cancellationRegistration = ct.Register( () => testContext.SyncProvider.ReleaseAll() );

        // The extension is discovered in the client before the service is added, so the client creates the service
        // client as soon as it learns about the service, without the retry path.
        var clientExtensionManager = new DesignTimeExtensionManager( testContext.Global.Underlying );
        clientExtensionManager.OnExtensionDiscovered( new Extension() );

        var pipeName = $"{nameof(RpcServiceProviderTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new RpcServiceProviderServerEndpoint( testContext.Global, pipeName, [] );
        serverEndpoint.Start();

        using var clientEndpoint = new RpcServiceProviderClientEndpoint( testContext.Global.WithService( clientExtensionManager ), pipeName );
        await clientEndpoint.ConnectAsync( ct );
        await clientEndpoint.WaitUntilInitializedAsync( ct );

        // Services added after startup are served on a second pipe. Pin the connection to that pipe after it has
        // registered the client by interface name but before it has published the client.
        var extensionPipeName = $"{pipeName}-2";
        var afterStartsListening = $"ClientEndpoint.AfterStartsListening:{extensionPipeName}";
        testContext.SyncProvider.EnableSyncPoint( afterStartsListening );

        var extensionServiceFactory = new ExtensionServiceFactory();
        serverEndpoint.AddServices( [extensionServiceFactory] );
        await testContext.SyncProvider.WaitForSyncPointReachedAsync( afterStartsListening, ct );

        // Pin the call after it has observed that the client is not published, before it registers its awaiter. The
        // sync point is synchronous and holds the add-client lock, so the call runs on a background task.
        var beforeRegisterAwaiter = $"ClientEndpoint.BeforeRegisterAwaiter:{pipeName}";
        testContext.SyncProvider.EnableSyncPoint( beforeRegisterAwaiter );
        var getApiTask = Task.Run( () => clientEndpoint.GetApiAsync<IExtensionService>( ct ).AsTask(), ct );
        await testContext.SyncProvider.WaitForSyncPointReachedAsync( beforeRegisterAwaiter, ct );

        // Let the call register its awaiter, then let the connection publish the client and signal the awaiters. The
        // order does not matter: the connection signals under the add-client lock, which the call holds until it has
        // registered its awaiter.
        testContext.SyncProvider.ReleaseSyncPoint( beforeRegisterAwaiter );
        testContext.SyncProvider.ReleaseSyncPoint( afterStartsListening );

        var api = await getApiTask.WithCancellation( ct );
        await api.HelloAsync().WithCancellation( ct );
        Assert.True( extensionServiceFactory.IsHelloMethodCalled );

        await clientEndpoint.WhenBackgroundTasksCompletedAsync( ct );
    }
}
