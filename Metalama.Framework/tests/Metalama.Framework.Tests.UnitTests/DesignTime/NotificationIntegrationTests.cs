// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc.Notifications;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime;

#pragma warning disable VSTHRD200

public sealed class NotificationIntegrationTests : DistributedDesignTimeTestBase
{
    public NotificationIntegrationTests( ITestOutputHelper logger ) : base( logger ) { }

    public async Task ReceivesNotification()
    {
        using var testContext = this.CreateDistributedDesignTimeTestContext( options: new TestContextOptions() { HasSourceGeneratorTouchFile = true } );

        await testContext.WhenFieldsInitialized;

        // Start the notification listener.
        var notificationListenerEndpoint = new CodeLensProcessClientEndpoint(
            testContext.ServiceProvider.Underlying,
            testContext.ServiceHubServerEndpoint.PipeName );

        // We need to make sure that the notification listener listens before we run the pipeline,
        // otherwise the notification will be missed.
        await notificationListenerEndpoint.ConnectAsync();
        await testContext.WhenFullyInitialized;

        BlockingCollection<CompilationResultChangedEventData> eventQueue = new();

        notificationListenerEndpoint.Client.EventReceived +=
            eventData =>
            {
                if ( eventData is CompilationResultChangedEventData compilationResultChangedEventData )
                {
                    eventQueue.Add( compilationResultChangedEventData );
                }
            };

        var compilation1 = testContext.CreateCSharpCompilation( "", assemblyName: "project" );
        var pipeline = testContext.PipelineFactory.GetOrCreatePipeline( testContext.ProjectOptions, compilation1 ).AssertNotNull();

        // The first pipeline execution should notify a full compilation.
        await pipeline.ExecuteAsync( compilation1, AsyncExecutionContext.Get() );
        var notification1 = eventQueue.Take( testContext.CancellationToken );

        Assert.False( notification1.IsPartialCompilation );

        var compilation2 = testContext.CreateCSharpCompilation( "class C{}", assemblyName: "project" );
        await pipeline.ExecuteAsync( compilation2, AsyncExecutionContext.Get() );
        var notification2 = eventQueue.Take( testContext.CancellationToken );

        Assert.True( notification2.IsPartialCompilation );
    }
}