// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.CodeLens;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Project;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime;

#pragma warning disable VSTHRD200

public sealed class CodeLensTests : DesignTimeTestBase
{
    public CodeLensTests( ITestOutputHelper? testOutputHelper ) : base( testOutputHelper ) { }

    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );
        services.AddGlobalService( provider => new TestWorkspaceProvider( provider ) );
    }

    [Fact]
    public async Task AspectAddingAspect()
    {
        using var testContext = this.CreateTestContext();
        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        const string code = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects; 
                            using Metalama.Framework.Code;
                            using System;
                            using System.IO;

                            public class InjectedLoggerAttribute : OverrideMethodAspect
                            {
                                [Introduce]
                                private readonly TextWriter _logger = Console.Out;
                            
                                public override dynamic? OverrideMethod()
                                {
                                    _logger.WriteLine("Logged.");
                                    return meta.Proceed();
                                }
                            }

                            public class RepositoryAspect : TypeAspect
                            {
                                public override void BuildAspect(IAspectBuilder<INamedType> builder)
                                {
                                    var introduced = builder.IntroduceMethod( nameof(Get) );
                            
                                    introduced.AddAspect<InjectedLoggerAttribute>();
                                }
                            
                                [Template]
                                public object Get(int id) => id.ToString();
                            }

                            [RepositoryAspect]
                            public class Repository
                            {
                            }
                            """;

        var workspaceProvider = factory.ServiceProvider.GetRequiredService<TestWorkspaceProvider>();
        var projectKey = workspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string> { ["code.cs"] = code } );

        var compilation = (await workspaceProvider.GetCompilationAsync( projectKey ))!;

        // We need to run the pipeline because code lens does not run it on its own.
        var pipeline = factory.CreatePipeline( compilation );
        await pipeline.ExecuteAsync( compilation, AsyncExecutionContext.Get() );

        // Test the CodeLens service.
        var targetClassId = compilation.GetTypeByMetadataName( "Repository" )!.GetSerializableId();

        var codeLensService = new CodeLensServiceImpl( factory.ServiceProvider );

        var summary = await codeLensService.GetCodeLensSummaryAsync( projectKey, targetClassId, default );

        Assert.Equal( "2 aspects", summary.Description );

        var details = await codeLensService.GetCodeLensDetailsAsync( projectKey, targetClassId, default );

        var detailsText = details.Entries.SelectAsArray( e => e.Fields.SelectAsArray( f => f.Text ) );

        Assert.Equal(
            [
                ["RepositoryAspect", "Repository", "Custom attribute", "Introduce method 'Get(int)' into type 'Repository'."],
                [
                    "InjectedLogger", "Repository.Get(int)", "Child of 'RepositoryAspect' on 'Repository'",
                    "Introduce field '_logger' into type 'Repository'."
                ]
            ],
            detailsText );
    }

    [Fact]
    public async Task SkippedAspect()
    {
        using var testContext = this.CreateTestContext();
        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        const string code = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects; 
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Diagnostics;

                            [assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(MyChildAspect), typeof(MyAspect))]

                            class MyChildAspect : TypeAspect
                            {
                                [Introduce]
                                int child;
                            }

                            public class MyAspect : TypeAspect
                            {
                                public MyAspect(bool skip)
                                {
                                    this.skip = skip;
                                }
                            
                                private readonly bool skip;
                            
                                [Introduce]
                                int i;
                            
                                public override void BuildAspect(IAspectBuilder<INamedType> builder)
                                {
                                    if (skip)
                                    {
                                        builder.SkipAspect();
                                    }
                            
                                    builder.AddAspect<MyChildAspect>();
                            
                                    builder.IntroduceMethod( nameof(Template));
                                }
                            
                                [Template]
                                void Template() { }
                            }

                            [MyAspect(skip: false)]
                            class C1;

                            [MyAspect(skip: true)]
                            class C2;
                            """;

        var workspaceProvider = factory.ServiceProvider.GetRequiredService<TestWorkspaceProvider>();
        var projectKey = workspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string> { ["code.cs"] = code } );

        var compilation = (await workspaceProvider.GetCompilationAsync( projectKey ))!;

        // We need to run the pipeline because code lens does not run it on its own.
        var pipeline = factory.CreatePipeline( compilation );
        await pipeline.ExecuteAsync( compilation, AsyncExecutionContext.Get() );

        // Test the CodeLens service.
        var c1Id = compilation.GetTypeByMetadataName( "C1" )!.GetSerializableId();
        var c2Id = compilation.GetTypeByMetadataName( "C2" )!.GetSerializableId();

        var codeLensService = new CodeLensServiceImpl( factory.ServiceProvider );

        var summary1 = await codeLensService.GetCodeLensSummaryAsync( projectKey, c1Id, default );

        Assert.Equal( "2 aspects", summary1.Description );

        var details1 = await codeLensService.GetCodeLensDetailsAsync( projectKey, c1Id, default );

        Assert.Equal(
            [
                ["MyAspect", "C1", "Custom attribute", "Introduce field 'i' into type 'C1'."],
                ["", "", "", "Introduce method 'Template()' into type 'C1'."],
                ["", "", "", "(The aspect also transforms 1 child declaration.)"],
                ["MyChildAspect", "C1", "Child of 'MyAspect' on 'C1'", "Introduce field 'child' into type 'C1'."]
            ],
            details1.Entries.SelectAsReadOnlyList( e => e.Fields.SelectAsReadOnlyList( f => f.Text ) ) );

        var summary2 = await codeLensService.GetCodeLensSummaryAsync( projectKey, c2Id, default );

        Assert.Equal( "0 aspects", summary2.Description );

        var details2 = await codeLensService.GetCodeLensDetailsAsync( projectKey, c2Id, default );

        Assert.Empty( details2.Entries );
    }
}