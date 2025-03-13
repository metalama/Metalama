// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.DesignTime.VisualStudio.Preview;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTestHelpers.TestClasses;

#pragma warning disable VSTHRD200

public abstract class PreviewTestsBase : DesignTimeTestBase
{
    private const string _mainProjectName = "master";

    protected PreviewTestsBase( ITestOutputHelper logger ) : base( logger ) { }

    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );
        services.AddGlobalService( provider => new TestWorkspaceProvider( provider ) );
    }

    protected override TestContextOptions CreateDefaultTestContextOptions() => new() { CodeFormattingOptions = CodeFormattingOptions.Formatted };

    protected async Task<string> RunPreviewAsync(
        Dictionary<string, string> code,
        string previewedSyntaxTreeName,
        Dictionary<string, string>? dependencyCode = null )
    {
        using var testContext = this.CreateTestContext();
        var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        return await RunPreviewAsync(
            testContext,
            testContext.ServiceProvider.Global.WithService( pipelineFactory ),
            code,
            previewedSyntaxTreeName,
            dependencyCode );
    }

    protected static async Task<string> RunPreviewAsync(
        TestContext testContext,
        GlobalServiceProvider serviceProvider,
        Dictionary<string, string> code,
        string previewedSyntaxTreeName,
        Dictionary<string, string>? dependencyCode = null )
    {
        string[]? references;

        var workspace = testContext.ServiceProvider.Global.GetRequiredService<TestWorkspaceProvider>();

        if ( dependencyCode != null )
        {
            workspace.AddOrUpdateProject( testContext, "dependency", dependencyCode );
            references = ["dependency"];
        }
        else
        {
            references = null;
        }

        var projectKey = workspace.AddOrUpdateProject( testContext, _mainProjectName, code, projectReferences: references );

        var service = new TransformationPreviewServiceImpl( serviceProvider );
        var result = await service.PreviewTransformationAsync( projectKey, previewedSyntaxTreeName );

        Assert.Empty( result.ErrorMessages ?? [] );
        Assert.True( result.IsSuccessful );
        Assert.NotNull( result.TransformedSyntaxTree );

        // In production, the formatting happens in the user process. For tests, we run it separately.
        var document = workspace.GetDocumentOrNull( _mainProjectName, previewedSyntaxTreeName )
                       ?? workspace.GetProject( _mainProjectName ).AddDocument( previewedSyntaxTreeName, string.Empty );

        var formattedDocument = await UserProcessTransformationPreviewService.FormatOutputAsync( document, result, testContext.CancellationToken );

        var text = await formattedDocument.GetTextAsync();

        var s = text.ToString();

        // Check that the output is formatted.
        Assert.DoesNotContain( "global::", s, StringComparison.Ordinal );

        return s;
    }
}