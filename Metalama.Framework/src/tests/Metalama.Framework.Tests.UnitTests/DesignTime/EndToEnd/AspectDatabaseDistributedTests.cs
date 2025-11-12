// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.AspectExplorer;
using Metalama.Framework.DesignTime.VisualStudio.AspectExplorer;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.EndToEnd;

#pragma warning disable VSTHRD200

public sealed class AspectDatabaseDistributedTests : DistributedDesignTimeTestBase
{
    public AspectDatabaseDistributedTests( ITestOutputHelper? testOutputHelper ) : base( testOutputHelper ) { }

    [Fact]
    public async Task EndToEnd()
    {
        // This test reproduces https://github.com/metalama/Metalama/issues/1183, which is linked to the serialization
        // in presence of successors.

        var analysisProcessServices = new ServiceProviderBuilder<IGlobalService>();

        // Initialize the components.
        using var testContext = this.CreateDistributedDesignTimeTestContext( null, analysisProcessServices );

        await testContext.WhenFullyInitialized;

        const string code = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects; 
                            using Metalama.Framework.Code;

                            [assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof( Aspect1 ), typeof( Aspect2 ) )]

                            class Aspect1 : TypeAspect
                            {
                                public override void BuildAspect(IAspectBuilder<INamedType> builder)
                                {
                                    builder.RequireAspect<Aspect2>();
                                }
                            }

                            class Aspect2 : TypeAspect
                            {
                               [Introduce]
                               void IntroducedMethod(){}
                            }

                            [Aspect1]
                            class TheClass {}
                            """;

        // Initialize the workspace.
        var projectKey = testContext.WorkspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string> { ["code.cs"] = code } );
        await testContext.AnalysisProcessEndpoint.RegisterProjectAsync( projectKey, testContext.CancellationToken );
        var compilation = (await testContext.WorkspaceProvider.GetCompilationAsync( projectKey ))!;

        // We need to run the pipeline because code lens does not run it on its own.
        var project = testContext.WorkspaceProvider.GetProject( "project" );
        var pipeline = testContext.PipelineFactory.GetOrCreatePipeline( project )!;
        var pipelineResult = await pipeline.ExecuteAsync( (await project.GetCompilationAsync())!, AsyncExecutionContext.Get() );
        Assert.True( pipelineResult.IsSuccessful );

        var aspectDatabaseService = new AspectDatabaseService( testContext.UserProcessServiceProvider );

        var aspectClasses = (await aspectDatabaseService.GetAspectClassesAsync( compilation, testContext.CancellationToken ))
            .OrderBy( x => x.Name )
            .ToArray();

        Assert.Equal( 2, aspectClasses.Length );

        var aspectInstances1 = new IEnumerable<IAspectExplorerAspectInstance>[1];
        await aspectDatabaseService.GetAspectInstancesAsync( compilation, aspectClasses[0], aspectInstances1, testContext.CancellationToken );

        Assert.Equal( 2, aspectInstances1[0].Count() );

        var aspectInstances2 = new IEnumerable<IAspectExplorerAspectInstance>[1];
        await aspectDatabaseService.GetAspectInstancesAsync( compilation, aspectClasses[1], aspectInstances2, testContext.CancellationToken );

        Assert.Single( aspectInstances2[0] );
    }
}