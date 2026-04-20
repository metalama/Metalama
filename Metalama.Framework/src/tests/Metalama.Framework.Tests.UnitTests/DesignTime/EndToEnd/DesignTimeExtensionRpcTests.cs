// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.EndToEnd;

#pragma warning disable VSTHRD200

/// <summary>
/// Tests that demonstrate the issue from GitHub issue #1242:
/// When a design-time extension adds RPC services, the client endpoint in the user process
/// waits forever for the extension to be discovered, but the extension is only discovered
/// in the analysis process during pipeline execution.
/// </summary>
public sealed class DesignTimeExtensionRpcTests : DistributedDesignTimeTestBase
{
    public DesignTimeExtensionRpcTests( ITestOutputHelper? testOutputHelper ) : base( testOutputHelper ) { }

    protected override void ConfigureExtensions( ITestExtensionCollector collector )
    {
        base.ConfigureExtensions( collector );
        collector.AddDesignTimeExtension( typeof(TestDesignTimeExtension) );
    }

    /// <summary>
    /// This test demonstrates the deadlock from issue #1242.
    /// When a design-time extension adds RPC services during initialization:
    /// 1. The analysis process discovers the extension during pipeline execution
    /// 2. The extension's Initialize method adds RPC services to the server
    /// 3. The client (user process) receives notification about new services
    /// 4. The client starts a background task that waits for the extension to be discovered
    /// 5. But the user process never discovers the extension → deadlock
    ///
    /// This test uses sync points to make the deadlock deterministic.
    /// </summary>
    [Fact]
    public async Task DesignTimeExtensionWithRpcService_DeadlockOnUserProcess()
    {
        using var testContext = this.CreateDistributedDesignTimeTestContext();

        // ReSharper disable once UseAwaitUsing
        using var cancellationRegistration = testContext.CancellationToken.Register( () => testContext.SyncProvider.ReleaseAll() );

        await testContext.WhenFullyInitialized;

        const string code = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Code;

                            class TheAspect : TypeAspect { }

                            class TheClass {}
                            """;

        // Initialize the workspace.
        var projectKey = testContext.WorkspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string> { ["code.cs"] = code } );

        // Initialize the user process - simulates what happens in production when code fix/refactoring providers run.
        testContext.InitializeUserProcessForProject( "project" );

        // Initialize the analysis process and register the project.
        await testContext.AnalysisProcessEndpoint.RegisterProjectAsync( projectKey, testContext.CancellationToken );

        // Enable the sync point BEFORE running the pipeline.
        // This will block the client when it tries to load services for the TestDesignTimeExtension.
        var syncPointName = $"RpcServiceProviderClientEndpoint.BeforeExtensionCheck:{TestDesignTimeExtension.ExtensionName}";
        testContext.SyncProvider.EnableSyncPoint( syncPointName );

        // Execute the pipeline - this triggers extension discovery in the analysis process only.
        // The extension's Initialize method adds RPC services to the server.
        // The server notifies the client (user process) about new services.
        // The client will block at the sync point before checking if the extension is loaded.
        var project = testContext.WorkspaceProvider.GetProject( "project" );
        var pipeline = testContext.PipelineFactory.GetOrCreatePipeline( project )!;
        await pipeline.ExecuteAsync( (await project.GetCompilationAsync( testContext.CancellationToken ))!, AsyncExecutionContext.Get() );

        // Wait for the client to reach the sync point.
        await testContext.SyncProvider.WaitForSyncPointReachedAsync( syncPointName, testContext.CancellationToken );

        // Release the sync point WITHOUT registering the extension in the user process.
        // This forces the client to enter the retry path where it waits for GetExtensionAsync.
        testContext.SyncProvider.ReleaseSyncPoint( syncPointName );

        // Get the client endpoint via the ServiceHub.
        var clientEndpoint = await testContext.UserProcessServiceHubEndpoint.ServiceHub.GetEndpointAsync( projectKey, testContext.CancellationToken );

        // Wait for background tasks to complete - this will timeout if there's a deadlock.
        // The test should PASS once the bug is fixed, and FAIL while the bug exists.
        using var timeoutCts = new CancellationTokenSource( TimeSpan.FromSeconds( 20 ) );
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource( timeoutCts.Token, testContext.CancellationToken );

        try
        {
            await clientEndpoint.WhenBackgroundTasksCompletedAsync( linkedCts.Token );
        }
        finally
        {
            // Clean up: release all sync points to avoid hanging on dispose.
            testContext.SyncProvider.ReleaseAll();
        }
    }

    /// <summary>
    /// Tests late user process initialization: the user process calls <see cref="DistributedDesignTimeTestContext.InitializeUserProcessForProject"/>
    /// AFTER the pipeline has run and <c>ServicesAddedEvent</c> was received.
    /// This tests the retry mechanism in <c>RpcServiceProviderClientEndpoint</c> within the full distributed context.
    /// </summary>
    [Fact]
    public async Task ExtensionRpc_UserProcessInitializedAfterPipeline()
    {
        using var testContext = this.CreateDistributedDesignTimeTestContext();

        // ReSharper disable once UseAwaitUsing
        using var cancellationRegistration = testContext.CancellationToken.Register( () => testContext.SyncProvider.ReleaseAll() );

        await testContext.WhenFullyInitialized;

        const string code = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Code;

                            class TheAspect : TypeAspect { }

                            class TheClass {}
                            """;

        // 1. Initialize workspace with project (but do NOT initialize user process yet)
        var projectKey = testContext.WorkspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string> { ["code.cs"] = code } );

        // Initialize the analysis process and register the project.
        await testContext.AnalysisProcessEndpoint.RegisterProjectAsync( projectKey, testContext.CancellationToken );

        // Enable the sync point BEFORE running the pipeline.
        var syncPointName = $"RpcServiceProviderClientEndpoint.BeforeExtensionCheck:{TestDesignTimeExtension.ExtensionName}";
        testContext.SyncProvider.EnableSyncPoint( syncPointName );

        // 2. Run pipeline FIRST - extension discovered in analysis process, RPC services added
        var project = testContext.WorkspaceProvider.GetProject( "project" );
        var pipeline = testContext.PipelineFactory.GetOrCreatePipeline( project )!;
        await pipeline.ExecuteAsync( (await project.GetCompilationAsync( testContext.CancellationToken ))!, AsyncExecutionContext.Get() );

        // 3. Wait for client to receive ServicesAddedEvent and start waiting for extension
        await testContext.SyncProvider.WaitForSyncPointReachedAsync( syncPointName, testContext.CancellationToken );

        // Release sync point WITHOUT initializing user process yet - this forces the retry path
        testContext.SyncProvider.ReleaseSyncPoint( syncPointName );

        // 4. NOW initialize user process - triggers the retry mechanism
        testContext.InitializeUserProcessForProject( "project" );

        // 5. Verify RPC services load successfully
        var clientEndpoint = await testContext.UserProcessServiceHubEndpoint.ServiceHub.GetEndpointAsync( projectKey, testContext.CancellationToken );

        using var timeoutCts = new CancellationTokenSource( TimeSpan.FromSeconds( 20 ) );
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource( timeoutCts.Token, testContext.CancellationToken );

        try
        {
            await clientEndpoint.WhenBackgroundTasksCompletedAsync( linkedCts.Token );

            // 6. Verify RPC call works
            var api = await clientEndpoint.GetApiAsync<ITestExtensionApi>( testContext.CancellationToken );
            await api.PingAsync();
        }
        finally
        {
            testContext.SyncProvider.ReleaseAll();
        }
    }

    /// <summary>
    /// Tests that two projects each with extensions are properly isolated.
    /// </summary>
    [Fact]
    public async Task ExtensionRpc_MultipleProjects_IsolatedExtensions()
    {
        using var testContext = this.CreateDistributedDesignTimeTestContext();

        // ReSharper disable once UseAwaitUsing
        using var cancellationRegistration = testContext.CancellationToken.Register( () => testContext.SyncProvider.ReleaseAll() );

        await testContext.WhenFullyInitialized;

        const string code1 = """
                             using Metalama.Framework.Advising;
                             using Metalama.Framework.Aspects;
                             using Metalama.Framework.Code;

                             class TheAspect1 : TypeAspect { }

                             class TheClass1 {}
                             """;

        const string code2 = """
                             using Metalama.Framework.Advising;
                             using Metalama.Framework.Aspects;
                             using Metalama.Framework.Code;

                             class TheAspect2 : TypeAspect { }

                             class TheClass2 {}
                             """;

        // 1. Add project1 and project2
        var projectKey1 = testContext.WorkspaceProvider.AddOrUpdateProject( testContext, "project1", new Dictionary<string, string> { ["code1.cs"] = code1 } );
        var projectKey2 = testContext.WorkspaceProvider.AddOrUpdateProject( testContext, "project2", new Dictionary<string, string> { ["code2.cs"] = code2 } );

        // 2. Initialize user process for both
        testContext.InitializeUserProcessForProject( "project1" );
        testContext.InitializeUserProcessForProject( "project2" );

        // 3. Register projects in analysis process
        await testContext.AnalysisProcessEndpoint.RegisterProjectAsync( projectKey1, testContext.CancellationToken );
        await testContext.AnalysisProcessEndpoint.RegisterProjectAsync( projectKey2, testContext.CancellationToken );

        // 4. Run pipelines for both
        var project1 = testContext.WorkspaceProvider.GetProject( "project1" );
        var project2 = testContext.WorkspaceProvider.GetProject( "project2" );

        await testContext.PipelineFactory.GetOrCreatePipeline( project1 )!.ExecuteAsync(
            (await project1.GetCompilationAsync( testContext.CancellationToken ))!,
            AsyncExecutionContext.Get() );

        await testContext.PipelineFactory.GetOrCreatePipeline( project2 )!.ExecuteAsync(
            (await project2.GetCompilationAsync( testContext.CancellationToken ))!,
            AsyncExecutionContext.Get() );

        // 5. Verify RPC services work for both projects independently
        var endpoint1 = await testContext.UserProcessServiceHubEndpoint.ServiceHub.GetEndpointAsync( projectKey1, testContext.CancellationToken );
        var endpoint2 = await testContext.UserProcessServiceHubEndpoint.ServiceHub.GetEndpointAsync( projectKey2, testContext.CancellationToken );

        using var timeoutCts = new CancellationTokenSource( TimeSpan.FromSeconds( 20 ) );
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource( timeoutCts.Token, testContext.CancellationToken );

        await endpoint1.WhenBackgroundTasksCompletedAsync( linkedCts.Token );
        await endpoint2.WhenBackgroundTasksCompletedAsync( linkedCts.Token );

        // 6. Both should have working RPC
        var api1 = await endpoint1.GetApiAsync<ITestExtensionApi>( testContext.CancellationToken );
        var api2 = await endpoint2.GetApiAsync<ITestExtensionApi>( testContext.CancellationToken );

        await api1.PingAsync();
        await api2.PingAsync();
    }
}

/// <summary>
/// Test design-time extension that adds an RPC service when initialized.
/// This extension is used to reproduce GitHub issue #1242.
/// </summary>
public sealed class TestDesignTimeExtension : IDesignTimeExtension
{
    public const string ExtensionName = "TestDesignTimeExtension";

    public string Name => ExtensionName;

    public bool Initialize( DesignTimeInitializationContext context )
    {
        // Configure shared services like Premium's CodeFixesDesignTimeExtension does.
        // This might trigger earlier service resolution.
        context.ConfigureSharedServices(
            serviceProvider => serviceProvider.Underlying
                .AddSharedService( _ => new TestSharedService() ) );

        // Only add RPC service in the analysis process (server side)
        if ( context.ProcessKind == DesignTimeProcessKind.VsAnalysisProcess )
        {
            context.AddRpcService( new TestExtensionServiceFactory() );
        }

        return true;
    }
}

/// <summary>
/// Test shared service to mimic Premium's CodeFixService.
/// </summary>
public sealed class TestSharedService : IGlobalService { }

/// <summary>
/// RPC API interface for the test extension service.
/// </summary>
public interface ITestExtensionApi : IRpcApi
{
    Task PingAsync();
}

/// <summary>
/// Factory that creates the RPC service and client.
/// </summary>
public sealed class TestExtensionServiceFactory : IRpcServiceFactory
{
    public string ExtensionName => TestDesignTimeExtension.ExtensionName;

    public RpcService CreateRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint endpoint ) => new TestExtensionService( endpoint );

    public RpcClient CreateRpcClient( GlobalServiceProvider serviceProvider, ClientEndpoint endpoint ) => new TestExtensionClient( endpoint );
}

/// <summary>
/// Server-side RPC service implementation.
/// </summary>
public sealed class TestExtensionService : RpcService<ITestExtensionApi>
{
    public TestExtensionService( ServerEndpoint endpoint ) : base( endpoint ) { }

    protected override ITestExtensionApi CreateApi( IRpcEventSender eventSender ) => new Api();

    private sealed class Api : ITestExtensionApi
    {
        public Task PingAsync() => Task.CompletedTask;
    }
}

/// <summary>
/// Client-side RPC client implementation.
/// </summary>
public sealed class TestExtensionClient : RpcClient<ITestExtensionApi>
{
    public TestExtensionClient( ClientEndpoint endpoint ) : base( endpoint ) { }
}