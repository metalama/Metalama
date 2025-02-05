// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.DiagnosticAnalysis;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.VersionNeutral;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTestHelpers.TestClasses;

#pragma warning disable VSTHRD200

public class SideBySideVersionTestsBase( ITestOutputHelper logger ) : DesignTimeTestBase( logger )
{
    private static async Task<(DesignTimeAspectPipelineFactory PipelineFactory, Compilation DependentCompilation, SyntaxTree DependentCodeTree)>
        PreparePipeline(
            TestContext testContext,
            string masterCode,
            string dependentCode )
    {
        var workspaceProvider = new TestWorkspaceProvider( testContext.ServiceProvider );

        var entryPointManager = new DesignTimeEntryPointManager();
        var consumer = entryPointManager.GetConsumer( CurrentContractVersions.All );

        var serviceProvider = testContext.ServiceProvider.Global.Underlying.WithUntypedService( typeof(IDesignTimeEntryPointConsumer), consumer )
            .WithService( workspaceProvider );

        // Initialize dependencies simulating the current version.
        var currentVersionPipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext, serviceProvider );
        var currentVersionServiceProvider = new CompilerServiceProvider( version: TestMetalamaProjectClassifier.CurrentMetalamaVersion );
        currentVersionServiceProvider.Initialize( serviceProvider.WithService( currentVersionPipelineFactory ) );
        entryPointManager.RegisterServiceProvider( currentVersionServiceProvider );

        // Initialize dependencies simulating the _other_ version.
        var otherVersionPipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext, serviceProvider );
        var otherVersionServiceProvider = new CompilerServiceProvider( version: TestMetalamaProjectClassifier.OtherMetalamaVersion );
        otherVersionServiceProvider.Initialize( serviceProvider.WithService( otherVersionPipelineFactory ) );
        entryPointManager.RegisterServiceProvider( otherVersionServiceProvider );

        workspaceProvider.AddOrUpdateProject(
            testContext,
            "master",
            new Dictionary<string, string>() { ["master.cs"] = masterCode },
            preprocessorSymbols: ["METALAMA", TestMetalamaProjectClassifier.OtherMetalamaVersionPreprocessorSymbol] );

        var dependentProjectKey = workspaceProvider.AddOrUpdateProject(
            testContext,
            "dependent",
            new Dictionary<string, string>() { ["dependent.cs"] = dependentCode },
            projectReferences: ["master"],
            preprocessorSymbols: ["METALAMA"] );

        var dependentCompilation = await workspaceProvider.GetCompilationAsync( dependentProjectKey );
        var dependentCodeSyntaxTree = await workspaceProvider.GetDocument( "dependent", "dependent.cs" ).GetSyntaxTreeAsync();

        return (currentVersionPipelineFactory, dependentCompilation, dependentCodeSyntaxTree);
    }

    protected async Task<FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>> RunPipeline( string masterCode, string dependentCode )
    {
        using var testContext = this.CreateTestContext();
        var (pipelineFactory, dependentCompilation, _) = await PreparePipeline( testContext, masterCode, dependentCode );

        return await pipelineFactory.ExecuteAsync( dependentCompilation, AsyncExecutionContext.Get() );
    }

    protected async Task<List<Diagnostic>> RunAnalyzer( string masterCode, string dependentCode )
    {
        using var testContext = this.CreateTestContext();
        var (pipelineFactory, dependentCompilation, dependentSyntaxTree) = await PreparePipeline( testContext, masterCode, dependentCode );

        var semanticModel = dependentCompilation.GetSemanticModel( dependentSyntaxTree );

        var analyzer = new TheDiagnosticAnalyzer( pipelineFactory.ServiceProvider );
        var analysisContext = new TestSemanticModelAnalysisContext( semanticModel, testContext.ProjectOptions );

        analyzer.AnalyzeSemanticModel( analysisContext );

        return analysisContext.ReportedDiagnostics;
    }
}