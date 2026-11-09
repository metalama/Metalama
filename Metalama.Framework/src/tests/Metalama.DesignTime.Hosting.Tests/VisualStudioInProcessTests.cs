// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.AspectExplorer;
using Metalama.Framework.DesignTime.Contracts.CodeLens;
using Metalama.Framework.DesignTime.Contracts.Diagnostics;
using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Threading;
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

    /// <summary>
    /// Changes the project that references another version of <c>Metalama.Framework</c> and no Metalama analyzer, and expects
    /// the analysis to succeed. Metalama must skip this project, as Roslyn does, instead of failing to configure its pipeline.
    /// </summary>
    [Fact]
    public async Task ProjectWithoutTheMetalamaAnalyzersIsNotAnalyzed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var workspace = this._fixture.Workspace;

        Assert.True(
            workspace.TryApplyChanges(
                workspace.CurrentSolution.WithDocumentText( this._fixture.OtherVersionDocumentId, SourceText.From( "public class D { public void M() { } }" ) ) ) );

        await VisualStudioInProcessHost.WaitForAnalysisAsync( cancellationToken );
    }

    /// <summary>
    /// Expects <see cref="DesignTimeHost.RunVisualStudioInProcessAsync"/> to reject an order of the projects, because Metalama
    /// analyzes the projects in solution order and the report would otherwise give an order that was not executed.
    /// </summary>
    [Fact]
    public async Task VisualStudioModeRejectsAProjectOrder()
    {
        var host = new DesignTimeHost( new DesignTimeHostOptions { Traversal = TraversalOrder.Reverse } );

        await Assert.ThrowsAsync<NotSupportedException>(
            () => host.RunVisualStudioInProcessAsync( this._fixture.Workspace, TestContext.Current.CancellationToken ) );
    }

    /// <summary>
    /// Changes the document that contains the aspect, and expects the user process to be told that compile-time code is being
    /// edited. Then completes the editing, and expects the user process to be told that it is complete, and the services to
    /// answer from the resumed pipeline. Then introduces a C# error in the aspect, completes the editing, and expects the
    /// error to reach the user process. Finally, restores the aspect.
    /// </summary>
    /// <remarks>
    /// The fixture gets <see cref="ICompileTimeEditingStatusService"/> and attaches the user interface before it gives the
    /// workspace to Metalama, as the Metalama extension of Visual Studio does when the services are registered. The test
    /// restores the aspect document and resumes the pipeline, so that the other tests observe the initial state.
    /// </remarks>
    [Fact( Timeout = 120_000 )]
    public async Task EditingStatusAndCompileTimeErrorsReachTheUserProcess()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var editingStatus = this._fixture.EditingStatus;
        var errorStatus = (ICompileTimeErrorStatusService?) this._fixture.ServiceProvider.GetService( typeof(ICompileTimeErrorStatusService) );
        Assert.NotNull( errorStatus );

        var editingStarted = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
        var editingCompleted = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
        var errorsChanged = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        void OnIsEditingChanged( bool isEditing ) => (isEditing ? editingStarted : editingCompleted).TrySetResult( true );

        void OnCompileTimeErrorsChanged()
        {
            if ( errorStatus.CompileTimeErrors.Length > 0 )
            {
                errorsChanged.TrySetResult( true );
            }
        }

        editingStatus.IsEditingChanged += OnIsEditingChanged;
        errorStatus.CompileTimeErrorsChanged += OnCompileTimeErrorsChanged;

        try
        {
            // Add an aspect class. The pipeline pauses.
            var newAspectCode = VisualStudioInProcessFixture.AspectCode
                                + "\n\npublic class OtherAttribute : OverrideMethodAspect\n{\n    public override dynamic? OverrideMethod() => meta.Proceed();\n}\n";

            await this.SetAspectCodeAsync( newAspectCode, cancellationToken );
            await WaitAsync( editingStarted.Task, cancellationToken );
            Assert.True( editingStatus.IsEditing );

            // Resume the pipeline, as the user does with the info bar.
            await this.CompleteEditingAsync( editingCompleted.Task, cancellationToken );
            Assert.False( editingStatus.IsEditing );

            // The services answer from the resumed pipeline.
            var compilation = (await this._fixture.Project.GetCompilationAsync( cancellationToken ))!;
            var summary = await this.GetCodeLensSummaryAsync( compilation, "M" );
            Assert.NotNull( summary );
            Assert.Equal( "1 aspect", summary.Description );

            // Introduce a C# error in the compile-time code. The pipeline pauses again, and reports the error when it resumes.
            editingStarted = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
            editingCompleted = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

            await this.SetAspectCodeAsync( newAspectCode.Replace( "public class OtherAttribute : OverrideMethodAspect\n{\n", "public class OtherAttribute : OverrideMethodAspect\n{\n    private int _field = \"\";\n" ), cancellationToken );
            await WaitAsync( editingStarted.Task, cancellationToken );
            await this.CompleteEditingAsync( editingCompleted.Task, cancellationToken );
            await WaitAsync( errorsChanged.Task, cancellationToken );

            Assert.Contains(
                errorStatus.CompileTimeErrors,
                e => e.Severity == DiagnosticSeverity.Error && e.FilePath != null && e.FilePath.EndsWith( "Aspect.cs", StringComparison.Ordinal ) );
        }
        finally
        {
            editingStatus.IsEditingChanged -= OnIsEditingChanged;
            errorStatus.CompileTimeErrorsChanged -= OnCompileTimeErrorsChanged;

            // Restore the aspect and resume the pipeline.
            await this.SetAspectCodeAsync( VisualStudioInProcessFixture.AspectCode, cancellationToken );
            await editingStatus.OnEditingCompletedAsync( cancellationToken );
            await VisualStudioInProcessHost.WaitForAnalysisAsync( cancellationToken );
        }
    }

    private async Task SetAspectCodeAsync( string code, CancellationToken cancellationToken )
    {
        var workspace = this._fixture.Workspace;

        Assert.True( workspace.TryApplyChanges( workspace.CurrentSolution.WithDocumentText( this._fixture.AspectDocumentId, SourceText.From( code ) ) ) );

        await VisualStudioInProcessHost.WaitForAnalysisAsync( cancellationToken );
    }

    private async Task CompleteEditingAsync( Task editingCompleted, CancellationToken cancellationToken )
    {
        await this._fixture.EditingStatus.OnEditingCompletedAsync( cancellationToken );
        await WaitAsync( editingCompleted, cancellationToken );
        await VisualStudioInProcessHost.WaitForAnalysisAsync( cancellationToken );
    }

    /// <summary>
    /// Waits for a task, and throws <see cref="OperationCanceledException"/> when the token is signalled first.
    /// </summary>
    private static async Task WaitAsync( Task task, CancellationToken cancellationToken )
    {
        var cancelled = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        using ( cancellationToken.Register( () => cancelled.TrySetResult( true ) ) )
        {
            await Task.WhenAny( task, cancelled.Task );
        }

        cancellationToken.ThrowIfCancellationRequested();

        await task;
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
