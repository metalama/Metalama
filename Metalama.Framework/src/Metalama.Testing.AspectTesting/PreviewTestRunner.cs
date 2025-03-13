// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.DesignTime;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting;

internal sealed class PreviewTestRunner : BaseTestRunner
{
    internal PreviewTestRunner(
        GlobalServiceProvider serviceProvider,
        string? projectDirectory,
        TestProjectReferences references,
        ITestOutputHelper? logger ) : base( serviceProvider, projectDirectory, references, logger ) { }

    protected override async Task RunAsync( TestInput testInput, TestResult testResult, TestContext testContext )
    {
        await base.RunAsync( testInput, testResult, testContext );

        var inputCompilation = testResult.InputCompilation.AssertNotNull();

        var (project, _) = await WorkspaceHelper.CreateProjectFromCompilationAsync(
            inputCompilation.WithOptions( (CSharpCompilationOptions) testResult.InputCompilation.Options ),
            testContext.CancellationToken );

        var workspace = new CustomWorkspace( project.Solution );

        var workspaceProvider = new LocalWorkspaceProvider( testContext.ServiceProvider.Global );
        Assert.True( workspaceProvider.TrySetWorkspace( workspace ) );

        var serviceProvider = testContext.ServiceProvider.Global;
        serviceProvider = serviceProvider.WithService( new DesignTimeExceptionHandler( serviceProvider ) );
        serviceProvider = serviceProvider.WithService( workspaceProvider );
        serviceProvider = serviceProvider.WithService( new DesignTimeAspectPipelineFactory( serviceProvider ) );

        var previewService = new TransformationPreviewServiceImpl( serviceProvider );

        var projectKey = ProjectKeyFactory.FromProject( project )!;

        var primarySyntaxTree = inputCompilation.SyntaxTrees.Single( t => t.FilePath == $"{testInput.TestName}.cs" );

        var primarySyntaxTreeName = primarySyntaxTree.FilePath;

        var targetSyntaxTreeName =
            testInput.Options.TargetSyntaxTreeSuffix != null
                ? primarySyntaxTreeName.ReplaceOrdinal( ".cs", $".{testInput.Options.TargetSyntaxTreeSuffix}.cs" )
                : primarySyntaxTreeName;

        var previewResult = await previewService.PreviewTransformationAsync(
            projectKey,
            targetSyntaxTreeName,
            testContext.CancellationToken );

        if ( !previewResult.IsSuccessful )
        {
            // This will throw an exception with the error messages.
            Assert.Empty( previewResult.ErrorMessages! );
            Assert.Fail( "The preview was not successful, but no message was reported." );
        }

        var transformedSyntaxTree = previewResult.TransformedSyntaxTree!.ToSyntaxTree( (CSharpParseOptions) primarySyntaxTree.Options )
            .WithFilePath( primarySyntaxTreeName );

        // In production code, the formatting is done by the caller, so we need to format here.
        var resultingCompilation = testContext.CreateEmptyCSharpCompilation( "result" ).AddSyntaxTrees( transformedSyntaxTree );
        var formattedResultingCompilation = await new CodeFormatter().FormatAllAsync( resultingCompilation, testContext.CancellationToken );

        await testResult.SetOutputCompilationAsync( formattedResultingCompilation );
        testResult.HasOutputCode = true;
    }
}