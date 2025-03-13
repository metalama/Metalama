// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime;

#pragma warning disable VSTHRD200

public sealed class CompileTimeErrorNotificationTests : DistributedDesignTimeTestBase
{
    public CompileTimeErrorNotificationTests( ITestOutputHelper? logger = null ) : base( logger ) { }

    [Fact]
    public async Task EndToEnd()
    {
        // Initialize the components.
        using var testContext = this.CreateDistributedDesignTimeTestContext();

        await testContext.WhenFullyInitialized;

        const string codeWithError = """
                                                 using Metalama.Framework.Advising;
                                                 using Metalama.Framework.Aspects; 
                                                 using Metalama.Framework.Code;
                                                 using System;
                                     
                                                 public class InjectedLoggerAttribute : OverrideMethodAspect
                                                 {
                                     
                                                     public override dynamic? OverrideMethod()
                                                     {
                                                         some error here
                                                     }
                                                 }

                                     """;

        // Initialize the workspace. We are initializing it with an error to check that we get the initial state correctly.
        var projectKey = testContext.WorkspaceProvider.AddOrUpdateProject(
            testContext,
            "project",
            new Dictionary<string, string> { ["code.cs"] = codeWithError } );

        await testContext.RpcServiceProviderEndpoint.RegisterProjectAsync( projectKey, testContext.CancellationToken );

        // Register a synchronization point.
        var hasCompilerErrorData = new TaskCompletionSource<bool>();

        testContext.ServiceHubServerEndpoint.ServiceHub.Endpoints.Single().EventReceived += eventData =>
        {
            if ( eventData is CompileTimeErrorsChangedEventData )
            {
                hasCompilerErrorData.TrySetResult( true );
            }
        };

        // Instantiate the CompileTimeEditingStatusService. It should start getting the messages.
        var errorService = new CompileTimeEditingStatusService( testContext.UserProcessServiceProvider );

        // Execute the pipeline to get the errors.
        var project = testContext.WorkspaceProvider.GetProject( "project" );
        var pipeline = testContext.PipelineFactory.GetOrCreatePipeline( project )!;
        var result = await pipeline.ExecuteAsync( (await project.GetCompilationAsync())!, AsyncExecutionContext.Get() );
        Assert.False( result.IsSuccessful );
        Assert.NotEmpty( result.Diagnostics );

        // Make sure we create CompileTimeEditingStatusService after the pipeline has first executed.
        await hasCompilerErrorData.Task;

        // Check that we have errors.
        Assert.NotEmpty( errorService.CompileTimeErrors );

        // Fix the error, run the pipeline, and check that the error collection is cleared.
        var hasCompilerErrorData2 = new TaskCompletionSource<bool>();
        errorService.CompileTimeErrorsChanged += () => hasCompilerErrorData2.SetResult( true );

        testContext.WorkspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string> { ["code.cs"] = "" } );

        var result2 = await pipeline.ExecuteAsync(
            (await testContext.WorkspaceProvider.GetProject( "project" ).GetCompilationAsync())!,
            AsyncExecutionContext.Get() );

        Assert.True( result2.IsSuccessful );
        Assert.Empty( result2.Diagnostics );

        await hasCompilerErrorData2.Task;
        Assert.Empty( errorService.CompileTimeErrors );
    }
}