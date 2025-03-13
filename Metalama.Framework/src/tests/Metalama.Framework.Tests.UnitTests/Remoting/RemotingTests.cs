// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Rpc.Notifications;
using Metalama.Framework.DesignTime.VisualStudio.Preview;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;
using Metalama.Framework.Engine.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Remoting;

#pragma warning disable VSTHRD200, VSTHRD103

public sealed class RemotingTests : UnitTestClass
{
    public RemotingTests( ITestOutputHelper logger ) : base( logger ) { }

    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );
        services.AddGlobalService( sp => new AnalysisProcessEventHub( sp ) );
        services.AddUntypedGlobalService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );
        services.AddUntypedGlobalService( typeof(IRpcExceptionHandler), new TestRpcExceptionHandler( this.TestOutput ) );
    }

    [Fact]
    public async Task PublishGeneratedSourceAfterHelloAsync()
    {
        using var testContext = this.CreateTestContext();
        var serviceProvider = testContext.ServiceProvider;
        var cancellationToken = testContext.CancellationToken;

        var projectKey = ProjectKeyFactory.CreateTest( "myProjectId" );
        const string sourceTreeName = "mySource";

        var pipeName = $"Metalama_Test_{Guid.NewGuid()}";

        using var server = new RpcServiceProviderServerEndpoint(
            serviceProvider,
            pipeName,
            [new SourceGeneratorRpcServiceFactory()] );

        using var clientEndpoint = new RpcServiceProviderClientEndpoint( serviceProvider, pipeName );
        var eventCollector = new RpcEventCollector();

        server.Start();
        await clientEndpoint.ConnectAsync( cancellationToken );

        clientEndpoint.EventReceived += eventCollector.OnEventReceived;

        var sourceGeneratorService = server.GetRequiredService<SourceGeneratorRpcService>();

        // If we don't wait until server initialization here, we could raise the event before the server is finished setting up.
        await server.WaitUntilInitializedAsync( cancellationToken );

        await sourceGeneratorService.PublishGeneratedSourcesAsync(
            projectKey,
            ImmutableDictionary.Create<string, string>().Add( sourceTreeName, "content" ),
            cancellationToken );

        await eventCollector.WhenTrueAsync( c => c.Events.OfType<GeneratedSourceChangedEventData>().Any(), cancellationToken );

        Assert.Single( eventCollector.Events.OfType<GeneratedSourceChangedEventData>(), x => x.ProjectKey == projectKey );
        Assert.Single( eventCollector.Events.OfType<GeneratedSourceChangedEventData>().First().Sources, x => x.Key == sourceTreeName );
    }

    [Fact]
    public async Task PublishGeneratedSourceBeforeHelloAsync()
    {
        using var testContext = this.CreateTestContext();
        var serviceProvider = testContext.ServiceProvider;
        var cancellationToken = testContext.CancellationToken;

        var projectKey = ProjectKeyFactory.CreateTest( "myProjectId" );
        const string sourceTreeName = "mySource";

        // Start the server.
        var pipeName = $"Metalama_Test_{Guid.NewGuid()}";

        using var server = new RpcServiceProviderServerEndpoint(
            serviceProvider,
            pipeName,
            [new SourceGeneratorRpcServiceFactory()] );

        server.Start();

        // Start the client and collect events.
        using var clientEndpoint = new RpcServiceProviderClientEndpoint( serviceProvider, pipeName );
        var eventCollector = new RpcEventCollector();
        clientEndpoint.EventReceived += eventCollector.OnEventReceived;

        // Connect.
        await clientEndpoint.ConnectAsync( cancellationToken );

        // Publish from the server.
        var sourceGeneratorService = server.GetRequiredService<SourceGeneratorRpcService>();

        await sourceGeneratorService.PublishGeneratedSourcesAsync(
            projectKey,
            ImmutableDictionary.Create<string, string>().Add( sourceTreeName, "content" ),
            cancellationToken );

        await eventCollector.WhenTrueAsync( c => c.Events.OfType<GeneratedSourceChangedEventData>().Any(), cancellationToken );

        // Asserts.
        Assert.Single( eventCollector.Events.OfType<GeneratedSourceChangedEventData>(), x => x.ProjectKey == projectKey );
        Assert.Single( eventCollector.Events.OfType<GeneratedSourceChangedEventData>().First().Sources, x => x.Key == sourceTreeName );
    }

    [Fact]
    public async Task PublishGeneratedSourceBeforeConnectAsync()
    {
        using var testContext = this.CreateTestContext();
        var serviceProvider = testContext.ServiceProvider;
        var cancellationToken = testContext.CancellationToken;

        var projectKey = ProjectKeyFactory.CreateTest( "myProjectId" );
        const string sourceTreeName = "mySource";

        // Start the server.
        var pipeName = $"Metalama_Test_{Guid.NewGuid()}";

        using var server = new RpcServiceProviderServerEndpoint(
            serviceProvider,
            pipeName,
            [new SourceGeneratorRpcServiceFactory()] );

        server.Start();

        // Publish from the server.
        var sourceGeneratorService = server.GetRequiredService<SourceGeneratorRpcService>();

        await sourceGeneratorService.PublishGeneratedSourcesAsync(
            projectKey,
            ImmutableDictionary.Create<string, string>().Add( sourceTreeName, "content" ),
            cancellationToken );

        // Start the client.
        using var clientEndpoint = new RpcServiceProviderClientEndpoint( serviceProvider, pipeName );
        var eventCollector = new RpcEventCollector();
        await clientEndpoint.ConnectAsync( cancellationToken );
        clientEndpoint.EventReceived += eventCollector.OnEventReceived;

        var sourceGeneratorClient = await clientEndpoint.GetClientAsync<SourceGeneratorRpcClient>( cancellationToken );

        // The cache must be empty.
        Assert.False( sourceGeneratorClient.TryGetCachedGeneratedSources( projectKey, out _ ) );

        // Get the data, which populates the cache.
        var generatedSources = await sourceGeneratorClient.GetGeneratedSourcesAsync( projectKey, cancellationToken );

        Assert.Single( generatedSources );
        Assert.Equal( sourceTreeName, generatedSources.Single().Key );

        // The cache is now populated.
        Assert.True( sourceGeneratorClient.TryGetCachedGeneratedSources( projectKey, out _ ) );
    }

    [Fact]
    public async Task TransformPreviewAsync()
    {
        using var testContext = this.CreateTestContext( new AdditionalServiceCollection( new PreviewImpl() ) );
        var serviceProvider = testContext.ServiceProvider;

        // Start the server.
        var pipeName = $"Metalama_Test_{Guid.NewGuid()}";

        using var server = new RpcServiceProviderServerEndpoint(
            serviceProvider,
            pipeName,
            [new PreviewTransformationRpcServiceFactory()] );

        server.Start();

        using var client = new RpcServiceProviderClientEndpoint( serviceProvider, pipeName );
        await client.ConnectAsync();

        var result = await (await client.GetApiAsync<IPreviewTransformationRpcApi>( testContext.CancellationToken )).PreviewTransformationAsync(
            ProjectKeyFactory.CreateTest( "myProjectId" ),
            "syntaxTreeName" );

        Assert.True( result.IsSuccessful );
        AssertEx.EolInvariantEqual( "class TransformedCode {}", result.TransformedSyntaxTree?.Text );
    }

    [Fact]
    public async Task RegisterEndpointAsync()
    {
        using var testContext = this.CreateTestContext();
        var serviceProvider = testContext.ServiceProvider.Global;

        var discoveryPipeName = $"Metalama_Test_Discovery_{Guid.NewGuid()}";
        using var userProcessHubEndpoint = new ServiceHubServerEndpoint( serviceProvider, discoveryPipeName );
        userProcessHubEndpoint.Start();

        using var processServiceHubEndpoint = new ServiceHubClientEndpoint( serviceProvider, discoveryPipeName );
        _ = processServiceHubEndpoint.ConnectAsync();

        var servicePipeName = $"Metalama_Test_Service_{Guid.NewGuid()}";

        using var analysisProcessEndpoint = new RpcServiceProviderServerEndpoint(
            serviceProvider.WithService( processServiceHubEndpoint ),
            servicePipeName,
            [] );

        analysisProcessEndpoint.Start();

        var projectKey = ProjectKeyFactory.CreateTest( "MyProjectId" );
        await analysisProcessEndpoint.RegisterProjectAsync( projectKey, testContext.CancellationToken );

        Assert.True( userProcessHubEndpoint.ServiceHub.IsProjectRegistered( projectKey ) );
    }

    [Fact]
    public async Task RegisterEndpoint_InvertedOrderAndDelayed()
    {
        using var testContext = this.CreateTestContext();
        var serviceProvider = testContext.ServiceProvider.Global;

        var discoveryPipeName = $"Metalama_Test_Discovery_{Guid.NewGuid()}";

        using var processServiceHubEndpoint = new ServiceHubClientEndpoint( serviceProvider, discoveryPipeName );
        _ = processServiceHubEndpoint.ConnectAsync();

        var servicePipeName = $"Metalama_Test_Service_{Guid.NewGuid()}";

        using var analysisProcessEndpoint = new RpcServiceProviderServerEndpoint(
            serviceProvider.WithService( processServiceHubEndpoint ),
            servicePipeName,
            [new SourceGeneratorRpcServiceFactory()] );

        analysisProcessEndpoint.Start();

        await Task.Delay( TimeSpan.FromSeconds( 5 ) );
        using var userProcessHubEndpoint = new ServiceHubServerEndpoint( serviceProvider, discoveryPipeName );
        userProcessHubEndpoint.Start();

        var projectKey = ProjectKeyFactory.CreateTest( "MyProjectId" );
        await analysisProcessEndpoint.RegisterProjectAsync( projectKey, testContext.CancellationToken );

        Assert.True( userProcessHubEndpoint.ServiceHub.IsProjectRegistered( projectKey ) );

        await userProcessHubEndpoint.ServiceHub.GetApiForProjectAsync<ISourceGeneratorRpcApi>( projectKey, testContext.CancellationToken );
    }

    [Fact]
    public async Task RegisterTwoEndpoints()
    {
        using var testContext = this.CreateTestContext();
        var serviceProvider = testContext.ServiceProvider.Global;

        var discoveryPipeName = $"Metalama_Test_Discovery_{Guid.NewGuid()}";
        using var userProcessHubEndpoint = new ServiceHubServerEndpoint( serviceProvider, discoveryPipeName );
        userProcessHubEndpoint.Start();

        var disposables = new List<IDisposable>();

        for ( var i = 0; i < 2; i++ )
        {
            var analysisProcessServiceHubEndpoint = new ServiceHubClientEndpoint( serviceProvider, discoveryPipeName );
            _ = analysisProcessServiceHubEndpoint.ConnectAsync();

            var servicePipeName = $"Metalama_Test_Service_{Guid.NewGuid()}";

            var analysisProcessEndpoint = new RpcServiceProviderServerEndpoint(
                serviceProvider.WithService( analysisProcessServiceHubEndpoint ),
                servicePipeName,
                [] );

            analysisProcessEndpoint.Start();

            var projectKey = ProjectKeyFactory.CreateTest( $"MyProjectId{i}" );
            await analysisProcessEndpoint.RegisterProjectAsync( projectKey, testContext.CancellationToken );

            Assert.True( userProcessHubEndpoint.ServiceHub.IsProjectRegistered( projectKey ) );

            disposables.Add( analysisProcessServiceHubEndpoint );
            disposables.Add( analysisProcessEndpoint );
        }

        // Dispose.
        disposables.Reverse();

        foreach ( var disposable in disposables )
        {
            disposable.Dispose();
        }
    }

    [Fact]
    public async Task RegisterTwoEndpoints_InvertedOrder()
    {
        using var testContext = this.CreateTestContext();
        var serviceProvider = testContext.ServiceProvider.Global;

        var discoveryPipeName = $"Metalama_Test_Discovery_{Guid.NewGuid()}";

        var registerTasks = new List<Task>();
        var projectKeys = new List<ProjectKey>();
        var disposables = new List<IDisposable>();

        const int clientCount = 2;

        for ( var i = 0; i < clientCount; i++ )
        {
            var analysisProcessServiceHubEndpoint = new ServiceHubClientEndpoint( serviceProvider, discoveryPipeName );

            disposables.Add( analysisProcessServiceHubEndpoint );

            _ = analysisProcessServiceHubEndpoint.ConnectAsync();

            var servicePipeName = $"Metalama_Test_Service_{Guid.NewGuid()}";

            var analysisProcessEndpoint = new RpcServiceProviderServerEndpoint(
                serviceProvider.WithService( analysisProcessServiceHubEndpoint ),
                servicePipeName,
                [new SourceGeneratorRpcServiceFactory()] );

            analysisProcessEndpoint.Start();

            var projectKey = ProjectKeyFactory.CreateTest( $"MyProjectId{i}" );

            registerTasks.Add( analysisProcessEndpoint.RegisterProjectAsync( projectKey, testContext.CancellationToken ) );
            projectKeys.Add( projectKey );
            disposables.Add( analysisProcessEndpoint );
        }

        await Task.Delay( TimeSpan.FromSeconds( 1 ) );

        using var userProcessHubEndpoint = new ServiceHubServerEndpoint( serviceProvider, discoveryPipeName );
        userProcessHubEndpoint.Start();

        await Task.WhenAll( registerTasks );

        foreach ( var projectKey in projectKeys )
        {
            Assert.True( userProcessHubEndpoint.ServiceHub.IsProjectRegistered( projectKey ) );
        }

        foreach ( var endPoint in userProcessHubEndpoint.ServiceHub.Endpoints )
        {
            // Should not wait forever.
            await endPoint.GetApiAsync<ISourceGeneratorRpcApi>( testContext.CancellationToken );
        }

        Assert.Equal( clientCount, userProcessHubEndpoint.ClientCount );

        // Dispose.
        disposables.Reverse();

        foreach ( var disposable in disposables )
        {
            disposable.Dispose();
        }
    }

    [Fact]
    public async Task MultipleConnect_Sequential()
    {
        using var testContext = this.CreateTestContext();
        var serviceProvider = testContext.ServiceProvider;

        var discoveryPipeName = $"Metalama_Test_Discovery_{Guid.NewGuid()}";

        // Connect the UserProcess endpoint.
        using var userProcessHubEndpoint = new ServiceHubServerEndpoint( serviceProvider, discoveryPipeName );
        userProcessHubEndpoint.Start();

        // Connect the AnalysisService endpoint.
        using var processServiceHubEndpoint = new ServiceHubClientEndpoint( serviceProvider, discoveryPipeName );
        Assert.True( await processServiceHubEndpoint.ConnectAsync() );

        // The second connect should not do anything.
        Assert.False( await processServiceHubEndpoint.ConnectAsync() );
    }

    [Fact]
    public async Task MultipleConnect_Concurrent()
    {
        using var testContext = this.CreateTestContext();
        var serviceProvider = testContext.ServiceProvider;

        var discoveryPipeName = $"Metalama_Test_Discovery_{Guid.NewGuid()}";

        // Connect the UserProcess endpoint.
        using var userProcessHubEndpoint = new ServiceHubServerEndpoint( serviceProvider, discoveryPipeName );
        userProcessHubEndpoint.Start();

        // Connect the AnalysisService endpoint.
        using var processServiceHubEndpoint = new ServiceHubClientEndpoint( serviceProvider, discoveryPipeName );
        var task1 = processServiceHubEndpoint.ConnectAsync();
        var task2 = processServiceHubEndpoint.ConnectAsync();

        await Task.WhenAll( task1, task2 );

        Assert.True( task1.Result );
        Assert.False( task2.Result );
    }

    [Fact]
    public async Task PublishChangeNotification()
    {
        using var testContext = this.CreateTestContext();
        var serviceProvider = testContext.ServiceProvider.Global;

        var discoveryPipeName = $"Metalama_Test_Discovery_{Guid.NewGuid()}";

        // Connect the UserProcess endpoint.
        using var userProcessHubEndpoint = new ServiceHubServerEndpoint( serviceProvider, discoveryPipeName );
        userProcessHubEndpoint.Start();

        // Connect the AnalysisService endpoint.
        using var processServiceHubEndpoint = new ServiceHubClientEndpoint( serviceProvider, discoveryPipeName );
        _ = processServiceHubEndpoint.ConnectAsync();

        // Connect the CodeLens endpoint.
        using var codeLensClientEndpoint = new CodeLensProcessClientEndpoint( serviceProvider.Underlying, discoveryPipeName );
        await codeLensClientEndpoint.ConnectAsync();

        var receivedNotificationTaskSource = new TaskCompletionSource<CompilationResultChangedEventData>();

        codeLensClientEndpoint.Client.EventReceived += eventData =>
        {
            if ( eventData is CompilationResultChangedEventData compilationResultChangedEventData )
            {
                receivedNotificationTaskSource.SetResult( compilationResultChangedEventData );
            }
        };

        var projectKey = ProjectKeyFactory.CreateTest( "MyProjectId" );
        var sentEventData = new CompilationResultChangedEventData( projectKey, false, ImmutableArray<string>.Empty );

        await userProcessHubEndpoint.EventHub.ForwardEventAsync( sentEventData, testContext.CancellationToken );

        var receivedNotification = await receivedNotificationTaskSource.Task.WithCancellation( testContext.CancellationToken );

        Assert.Equal( sentEventData.ProjectKey, receivedNotification.ProjectKey );
    }

    private sealed class PreviewImpl : ITransformationPreviewServiceImpl
    {
        public Task<SerializablePreviewTransformationResult> PreviewTransformationAsync(
            ProjectKey projectKey,
            string syntaxTreeName,
            CancellationToken cancellationToken )
        {
            return Task.FromResult(
                new SerializablePreviewTransformationResult(
                    true,
                    JsonSerializationHelper.CreateSerializableSyntaxTree( CSharpSyntaxTree.ParseText( "class TransformedCode {}" ) ),
                    null ) );
        }
    }
}