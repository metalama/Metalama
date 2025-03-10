// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.CodeLens;
using Metalama.Framework.DesignTime.VisualStudio.CodeLens;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.EndToEnd;

#pragma warning disable VSTHRD200

public sealed class CodeLensTests : DistributedDesignTimeTestBase
{
    public CodeLensTests( ITestOutputHelper? testOutputHelper ) : base( testOutputHelper ) { }

    [Fact]
    public async Task EndToEnd()
    {
        var analysisProcessServices = new ServiceProviderBuilder<IGlobalService>();

        // Initialize the components.
        using var testContext = this.CreateDistributedDesignTimeTestContext( null, analysisProcessServices );

        await testContext.WhenFullyInitialized;

        const string code = """
                            using Metalama.Framework.Advising; 
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects; 
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Code;

                            class TheAspect : TypeAspect
                            {
                               [Introduce]
                               void IntroducedMethod(){}
                            }

                            [TheAspect
                            class TheClass {}
                            """;

        // Initialize the workspace.
        var projectKey = testContext.WorkspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string> { ["code.cs"] = code } );
        await testContext.RpcServiceProviderEndpoint.RegisterProjectAsync( projectKey, testContext.CancellationToken );
        var compilation = (await testContext.WorkspaceProvider.GetCompilationAsync( projectKey ))!;

        // We need to run the pipeline because code lens does not run it on its own.
        var project = testContext.WorkspaceProvider.GetProject( "project" );
        var pipeline = testContext.PipelineFactory.GetOrCreatePipeline( project )!;
        await pipeline.ExecuteAsync( (await project.GetCompilationAsync())!, AsyncExecutionContext.Get() );

        // Test the CodeLens service.
        var theClassSymbol = compilation.GetTypeByMetadataName( "TheClass" )!;

        var codeLensService = new CodeLensService( testContext.UserProcessServiceProvider );

        var summary = new ICodeLensSummary[1];
        await codeLensService.GetCodeLensSummaryAsync( compilation, theClassSymbol, summary );

        Assert.NotNull( summary[0] );
        Assert.Equal( "1 aspect", summary[0].Description );

        var details = new ICodeLensDetails[1];
        await codeLensService.GetCodeLensDetailsAsync( compilation, theClassSymbol, details );

        Assert.NotNull( details[0] );
        var table = (ICodeLensDetailsTable) details[0];
        Assert.Single( table.Entries );
        Assert.NotNull( table.Entries[0].Fields[0] );
    }
}