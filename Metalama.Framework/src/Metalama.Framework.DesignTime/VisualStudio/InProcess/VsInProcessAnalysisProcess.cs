// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.Contracts.Workspaces;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.VisualStudio.Services;
using Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SharpCrafters.Backstage.Diagnostics;

namespace Metalama.Framework.DesignTime.VisualStudio.InProcess;

/// <summary>
/// Hosts, in the Visual Studio user process, the analysis process of the Visual Studio design-time architecture. It is created
/// only when <see cref="DesignTimeHostOverride.IsVisualStudioInProcess"/> is <c>true</c>.
/// </summary>
/// <remarks>
/// <para>
/// The analysis process has its own <see cref="GlobalServiceProvider"/>, built by <see cref="VsAnalysisProcessServiceProviderFactory"/>
/// from a separate set of global services, and its own <see cref="DesignTimeEntryPointManager"/>, so that the only
/// service provider registered in <see cref="DesignTimeEntryPointManager.Instance"/> is the one of the user process. The two
/// providers communicate through the named pipes that they use in Visual Studio. The names of these pipes include the
/// identifier of the current process, which makes them unique.
/// </para>
/// <para>
/// In Visual Studio, Roslyn runs the source generator of the analysis process whenever the solution changes, and this
/// execution runs the aspect pipeline and registers the project with the user process. This class does the same: it runs
/// <see cref="VsAnalysisProcessSourceGenerator"/> for every C# project of the workspace when it receives the workspace and
/// whenever the workspace changes.
/// </para>
/// <para>
/// The class implements <see cref="IWorkspaceReceiver"/>. The service provider of the user process returns it for this
/// interface, which is how the Visual Studio extension gives its workspace to Metalama. The workspace is given to the
/// workspace providers of both processes.
/// </para>
/// <para>
/// A test waits for the analysis with the delegate that <see cref="WaitForAnalysisAsync"/> publishes in the data slot
/// <see cref="WaitForAnalysisDataSlotName"/> of the current <see cref="AppDomain"/>. The delegate has the type
/// <c>Func&lt;CancellationToken, Task&gt;</c>, so that the test can call it without referencing Metalama.
/// </para>
/// </remarks>
internal sealed class VsInProcessAnalysisProcess : IGlobalService, ICompilerService, IWorkspaceReceiver
{
    /// <summary>
    /// The name of the <see cref="AppDomain"/> data slot that holds the <c>Func&lt;CancellationToken, Task&gt;</c> delegate
    /// returning a task that completes when the analysis of the current solution of the workspace is complete.
    /// </summary>
    public const string WaitForAnalysisDataSlotName = "Metalama.Framework.DesignTime.VisualStudioInProcess.WaitForAnalysis";

    private static readonly object _dataSlotSync = new();

    private readonly object _sync = new();
    private readonly LocalWorkspaceProvider _userProcessWorkspaceProvider;
    private readonly LocalWorkspaceProvider _analysisProcessWorkspaceProvider;
    private readonly VsAnalysisProcessProjectSourceGeneratorFactory _projectSourceGeneratorFactory;
    private readonly ISourceGenerator _sourceGenerator;
    private readonly ILogger _logger;
    private readonly CancellationToken _applicationExitingToken;
    private readonly TaskCompletionSource<Workspace> _workspaceTask = new( TaskCreationOptions.RunContinuationsAsynchronously );
    private Workspace? _workspace;
    private Solution? _lastScheduledSolution;
    private Task _pendingAnalysis = Task.CompletedTask;

    public VsInProcessAnalysisProcess( GlobalServiceProvider userProcessServiceProvider )
    {
        this._logger = userProcessServiceProvider.GetLoggerFactory().GetLogger( nameof(VsInProcessAnalysisProcess) );
        this._userProcessWorkspaceProvider = userProcessServiceProvider.GetRequiredService<LocalWorkspaceProvider>();

        // The analysis process gets its own global services, as if it were another process. Backstage is shared, because it
        // is initialized once per process.
        var analysisProcessFactory = new VsAnalysisProcessServiceProviderFactory( new DesignTimeEntryPointManager() );
        GlobalServiceProvider analysisProcessServiceProvider = analysisProcessFactory.GetServiceProvider( ServiceProviderFactory.GetServiceProvider( null ) );
        this.AnalysisProcessServiceProvider = analysisProcessServiceProvider;

        this._analysisProcessWorkspaceProvider = (LocalWorkspaceProvider) analysisProcessServiceProvider.GetRequiredService<WorkspaceProvider>();
        this._projectSourceGeneratorFactory = analysisProcessServiceProvider.GetRequiredService<VsAnalysisProcessProjectSourceGeneratorFactory>();
        this._applicationExitingToken = analysisProcessServiceProvider.GetRequiredService<ApplicationExitManager>().Token;
        this._sourceGenerator = new VsAnalysisProcessSourceGenerator( analysisProcessServiceProvider.Underlying ).AsSourceGenerator();

        this._logger.Trace?.Log( "The in-process analysis process has been created." );
    }

    /// <summary>
    /// Gets the service provider of the analysis process.
    /// </summary>
    public GlobalServiceProvider AnalysisProcessServiceProvider { get; }

    public void SetWorkspace( Workspace workspace )
    {
        lock ( this._sync )
        {
            if ( this._workspace != null )
            {
                if ( this._workspace != workspace )
                {
                    this._logger.Warning?.Log( "The workspace has already been set to a different instance. The new workspace is ignored." );
                }

                return;
            }

            this._workspace = workspace;
        }

        this._logger.Trace?.Log( "Received the workspace." );

        this._userProcessWorkspaceProvider.TrySetWorkspace( workspace );
        this._analysisProcessWorkspaceProvider.TrySetWorkspace( workspace );

        workspace.RegisterWorkspaceChangedHandler( _ => this.ScheduleAnalysis() );

        this._workspaceTask.TrySetResult( workspace );
        this.PublishWaitDelegate();

        this.ScheduleAnalysis();
    }

    /// <summary>
    /// Returns a task that completes when the analysis process has analyzed the current solution of the workspace, and when
    /// the background work that this analysis started is complete. When the workspace has not been received yet, the task
    /// waits for it.
    /// </summary>
    public async Task WaitForAnalysisAsync( CancellationToken cancellationToken )
    {
        // When the workspace has not been received yet, wait for it.
        await this._workspaceTask.Task.WithCancellation( cancellationToken );

        await this.ScheduleAnalysis().WithCancellation( cancellationToken );
    }

    /// <summary>
    /// Schedules the analysis of the current solution of the workspace, unless it is already scheduled, and returns the task of
    /// this analysis.
    /// </summary>
    /// <remarks>
    /// The method compares the current solution with the last scheduled one, so it does not depend on the order in which
    /// the workspace raises its change events. A caller that changes the workspace and then calls this method gets a task
    /// that includes the analysis of the change.
    /// </remarks>
    private Task ScheduleAnalysis()
    {
        lock ( this._sync )
        {
            var workspace = this._workspace;

            if ( workspace == null )
            {
                return Task.CompletedTask;
            }

            var solution = workspace.CurrentSolution;

            if ( solution == this._lastScheduledSolution )
            {
                return this._pendingAnalysis;
            }

            this._lastScheduledSolution = solution;

            var previousAnalysis = this._pendingAnalysis;

            // The analyses run one after the other, in the order of the solution versions.
            this._pendingAnalysis = Task.Run( () => this.AnalyzeSolutionAfterAsync( previousAnalysis, solution ) );

            return this._pendingAnalysis;
        }
    }

    private async Task AnalyzeSolutionAfterAsync( Task previousAnalysis, Solution solution )
    {
        try
        {
            await previousAnalysis;
        }
        catch ( Exception e ) when ( e is not OperationCanceledException )
        {
            // The failure of the previous analysis has already been reported by the task that awaited it.
        }

        await this.AnalyzeSolutionAsync( solution, this._applicationExitingToken );
    }

    private async Task AnalyzeSolutionAsync( Solution solution, CancellationToken cancellationToken )
    {
        foreach ( var project in solution.Projects )
        {
            if ( project.Language != LanguageNames.CSharp || !project.SupportsCompilation )
            {
                continue;
            }

            try
            {
                await this.AnalyzeProjectAsync( project, cancellationToken );
            }
            catch ( Exception e ) when ( e is not OperationCanceledException )
            {
                this._logger.Error?.Log( $"The analysis of the project '{project.Name}' failed: {e}" );

                throw;
            }
        }

        // The source generator schedules background work, for instance the registration of the project with the user process
        // and the publication of the generated sources. The analysis is complete only when this work is complete.
        await this._projectSourceGeneratorFactory.WaitForPendingTasksAsync( cancellationToken );
    }

    /// <summary>
    /// Runs the source generator of the analysis process for a project, as Roslyn does in the analysis process of Visual Studio.
    /// </summary>
    private async Task AnalyzeProjectAsync( Microsoft.CodeAnalysis.Project project, CancellationToken cancellationToken )
    {
        var compilation = await project.GetCompilationAsync( cancellationToken );

        if ( compilation == null )
        {
            return;
        }

        // The compilation of the workspace includes the sources generated by the generators of the user process. The
        // generator of the analysis process must not see them, because Roslyn gives it the compilation without generated sources.
        foreach ( var generatedDocument in await project.GetSourceGeneratedDocumentsAsync( cancellationToken ) )
        {
            var generatedSyntaxTree = await generatedDocument.GetSyntaxTreeAsync( cancellationToken );

            if ( generatedSyntaxTree != null && compilation.ContainsSyntaxTree( generatedSyntaxTree ) )
            {
                compilation = compilation.RemoveSyntaxTrees( generatedSyntaxTree );
            }
        }

        this._logger.Trace?.Log( $"Running the source generator of the analysis process for the project '{project.Name}'." );

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [this._sourceGenerator],
            project.AnalyzerOptions.AdditionalFiles,
            (CSharpParseOptions?) project.ParseOptions,
            project.AnalyzerOptions.AnalyzerConfigOptionsProvider );

        driver = driver.RunGenerators( compilation, cancellationToken );

        var exception = driver.GetRunResult().Results.Select( r => r.Exception ).FirstOrDefault( e => e != null );

        if ( exception != null )
        {
            throw new InvalidOperationException( $"The source generator of the analysis process failed for the project '{project.Name}'.", exception );
        }
    }

    /// <summary>
    /// Publishes <see cref="WaitForAnalysisAsync"/> in the data slot <see cref="WaitForAnalysisDataSlotName"/>.
    /// </summary>
    /// <remarks>
    /// Each Metalama version loaded in the process has its own instance of this class. When the slot already holds the
    /// delegate of another version, the new delegate waits for both.
    /// </remarks>
    private void PublishWaitDelegate()
    {
        lock ( _dataSlotSync )
        {
            Func<CancellationToken, Task> waitDelegate = this.WaitForAnalysisAsync;

            if ( AppDomain.CurrentDomain.GetData( WaitForAnalysisDataSlotName ) is Func<CancellationToken, Task> existingDelegate )
            {
                var ownDelegate = waitDelegate;
                waitDelegate = ct => Task.WhenAll( existingDelegate( ct ), ownDelegate( ct ) );
            }

            AppDomain.CurrentDomain.SetData( WaitForAnalysisDataSlotName, waitDelegate );
        }
    }
}
