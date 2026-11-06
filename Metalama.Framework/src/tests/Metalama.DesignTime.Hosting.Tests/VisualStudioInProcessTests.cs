// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.AspectExplorer;
using Metalama.Framework.DesignTime.Contracts.CodeLens;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

[assembly: AssemblyFixture( typeof(Metalama.DesignTime.Hosting.Tests.VisualStudioInProcessFixture) )]

namespace Metalama.DesignTime.Hosting.Tests;

/// <summary>
/// Tests that the services that the Metalama extension of Visual Studio consumes answer from the real design-time pipeline,
/// when Metalama hosts both Visual Studio processes in the test process.
/// </summary>
public sealed class VisualStudioInProcessTests
{
    private readonly VisualStudioInProcessFixture _fixture;

    public VisualStudioInProcessTests( VisualStudioInProcessFixture fixture )
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task AspectDatabaseReturnsTheAspectClass()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var compilation = (await this._fixture.Project.GetCompilationAsync( cancellationToken ))!;

        var aspectDatabase = (IAspectDatabaseService?) this._fixture.ServiceProvider.GetService( typeof(IAspectDatabaseService) );
        Assert.NotNull( aspectDatabase );

        var aspectClasses = await aspectDatabase.GetAspectClassesAsync( compilation, cancellationToken );

        Assert.Equal( ["LogAttribute"], aspectClasses.Select( c => c.Name ) );
    }

    [Fact]
    public async Task CodeLensReturnsTheSummaryOfAnAspectTarget()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var compilation = (await this._fixture.Project.GetCompilationAsync( cancellationToken ))!;

        var summary = await GetCodeLensSummaryAsync( compilation, "M" );

        Assert.NotNull( summary );
        Assert.Equal( "1 aspect", summary.Description );
    }

    /// <summary>
    /// Adds a method to which the aspect applies, waits for the analysis, and expects CodeLens to know the new method.
    /// </summary>
    /// <remarks>
    /// The other tests observe the method <c>M</c> only, so this change does not affect them. The change is made to the document
    /// that contains run-time code only, because a change to compile-time code pauses the pipeline.
    /// </remarks>
    [Fact]
    public async Task ServicesFollowAWorkspaceChange()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var workspace = this._fixture.Workspace;

        var newCode = VisualStudioInProcessFixture.Code.Replace( "public void M() { }", "public void M() { }\n\n    [Log]\n    public void N() { }" );
        Assert.True( workspace.TryApplyChanges( workspace.CurrentSolution.WithDocumentText( this._fixture.DocumentId, SourceText.From( newCode ) ) ) );

        await VisualStudioInProcessHost.WaitForAnalysisAsync( cancellationToken );

        var compilation = (await this._fixture.Project.GetCompilationAsync( cancellationToken ))!;
        var summary = await GetCodeLensSummaryAsync( compilation, "N" );

        Assert.NotNull( summary );
        Assert.Equal( "1 aspect", summary.Description );
    }

    private async Task<ICodeLensSummary?> GetCodeLensSummaryAsync( Compilation compilation, string methodName )
    {
        var codeLens = (ICodeLensService?) this._fixture.ServiceProvider.GetService( typeof(ICodeLensService) );
        Assert.NotNull( codeLens );

        var method = compilation.GetTypeByMetadataName( "C" )!.GetMembers( methodName ).Single();
        var result = new ICodeLensSummary?[1];
        await codeLens.GetCodeLensSummaryAsync( compilation, method, result, TestContext.Current.CancellationToken );

        return result[0];
    }
}
