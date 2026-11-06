// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.DesignTime.Hosting;

/// <summary>
/// Analyzes the projects of a Roslyn solution in a given order, the way an IDE does.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RunAsync"/> loads the analyzers in the current process and runs them, which is what Rider and the Visual Studio
/// Code language server do. <see cref="RunVisualStudioInProcessAsync"/> lets Metalama host both processes of the Visual Studio
/// architecture in the current process. See <see cref="VisualStudioInProcessHost"/>.
/// </para>
/// <para>
/// The solution can come from any workspace, for instance an <c>MSBuildWorkspace</c> or an <see cref="AdhocWorkspace"/>.
/// </para>
/// </remarks>
public sealed class DesignTimeHost
{
    private readonly DesignTimeHostOptions _options;

    public DesignTimeHost( DesignTimeHostOptions? options = null )
    {
        this._options = options ?? new DesignTimeHostOptions();
    }

    /// <summary>
    /// Analyzes every project of the solution, in the order given by the options, with analyzers loaded by a
    /// <see cref="HostAnalyzerAssemblyLoader"/>.
    /// </summary>
    public async Task<SimulationReport> RunAsync( Solution solution, CancellationToken cancellationToken )
    {
        var report = new SimulationReport();
        var loader = HostAnalyzerAssemblyLoader.Create();

        var projects = this.OrderProjects( solution );
        report.ProjectOrder = projects.Select( p => p.Name ).ToImmutableArray();

        foreach ( var project in projects )
        {
            cancellationToken.ThrowIfCancellationRequested();

            var session = new ProjectDesignTimeSession( project, loader, report );
            await session.RunAsync( cancellationToken );
        }

        report.LoadContextCount = loader.LoadContextCount;

        return report;
    }

    /// <summary>
    /// Analyzes the projects of a workspace with the Visual Studio architecture, both processes of which Metalama hosts in the
    /// current process. <see cref="VisualStudioInProcessHost.Enable"/> must have been called before any Metalama assembly was
    /// loaded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method does what Visual Studio and the Metalama extension do. It creates the Metalama source generators of each
    /// project in the user process, which registers the Metalama services. It gives the workspace to Metalama, which then
    /// runs the analysis process for each project. It waits until the analysis is complete. Finally, it reports the
    /// diagnostics of each compilation, which includes the sources that the generators of the user process produced.
    /// </para>
    /// <para>
    /// The analyzer references of the workspace are used as they are, so their assemblies are loaded by the loader of the
    /// workspace. A failure of the analysis is reported as an error of the <c>&lt;analysis&gt;</c> project.
    /// </para>
    /// </remarks>
    public async Task<SimulationReport> RunVisualStudioInProcessAsync( Workspace workspace, CancellationToken cancellationToken )
    {
        if ( !VisualStudioInProcessHost.IsEnabled )
        {
            throw new InvalidOperationException( $"{nameof(VisualStudioInProcessHost)}.{nameof(VisualStudioInProcessHost.Enable)} has not been called." );
        }

        var report = new SimulationReport();
        var projects = this.OrderProjects( workspace.CurrentSolution );
        report.ProjectOrder = projects.Select( p => p.Name ).ToImmutableArray();

        var metalamaProjectCount = 0;

        foreach ( var project in projects )
        {
            var projectReport = report.GetOrAddProject( project.Name );

            try
            {
                var generatorCount = await VisualStudioInProcessHost.CreateGeneratorsAsync( project );
                projectReport.GeneratorCount = generatorCount;

                if ( generatorCount > 0 )
                {
                    metalamaProjectCount++;
                }
            }
            catch ( Exception exception ) when ( exception is not OperationCanceledException )
            {
                projectReport.AddError( $"Creating the Metalama source generators threw {exception.GetType().Name}: {exception.Message}" );
            }
        }

        if ( metalamaProjectCount == 0 )
        {
            report.GetOrAddProject( "<analysis>" ).AddError( "No project references Metalama, so Metalama registered no service." );

            return report;
        }

        try
        {
            VisualStudioInProcessHost.SetWorkspace( workspace );
            await VisualStudioInProcessHost.WaitForAnalysisAsync( cancellationToken );
        }
        catch ( Exception exception ) when ( exception is not OperationCanceledException )
        {
            report.GetOrAddProject( "<analysis>" ).AddError( $"The analysis threw {exception.GetType().Name}: {exception}" );
        }

        var solution = workspace.CurrentSolution;

        foreach ( var project in projects.Select( p => solution.GetProject( p.Id ) ).Where( p => p != null ).Select( p => p! ) )
        {
            var projectReport = report.GetOrAddProject( project.Name );

            try
            {
                var compilation = await project.GetCompilationAsync( cancellationToken );

                if ( compilation == null )
                {
                    projectReport.AddError( $"The project '{project.Name}' produced no compilation." );

                    continue;
                }

                projectReport.GeneratedDocumentCount = (await project.GetSourceGeneratedDocumentsAsync( cancellationToken )).Count();
                projectReport.AddDiagnostics( compilation.GetDiagnostics( cancellationToken ) );
            }
            catch ( Exception exception ) when ( exception is not OperationCanceledException )
            {
                projectReport.AddError( $"Getting the diagnostics of '{project.Name}' threw {exception.GetType().Name}: {exception.Message}" );
            }
        }

        return report;
    }

    /// <summary>
    /// Returns the projects to analyze, in the order given by the options.
    /// </summary>
    public ImmutableArray<Project> OrderProjects( Solution solution )
    {
        var explicitOrder = this._options.OrderedProjectNames;

        if ( !explicitOrder.IsDefaultOrEmpty )
        {
            return OrderByName( solution, explicitOrder );
        }

        return this._options.Traversal switch
        {
            TraversalOrder.Graph => OrderByDependencyGraph( solution, reverse: false ),
            TraversalOrder.Reverse => OrderByDependencyGraph( solution, reverse: true ),
            _ => solution.Projects.ToImmutableArray()
        };
    }

    /// <summary>
    /// Returns the projects in dependency order, or in the exact opposite order.
    /// </summary>
    /// <remarks>
    /// <see cref="ProjectDependencyGraph.GetTopologicallySortedProjects(CancellationToken)"/> returns the projects so that a
    /// project comes after those it references, which is the order a batch build uses. Reversing it produces the order an
    /// editor routinely produces, where a dependent project is analyzed first.
    /// </remarks>
    private static ImmutableArray<Project> OrderByDependencyGraph( Solution solution, bool reverse )
    {
        var sorted = solution.GetProjectDependencyGraph().GetTopologicallySortedProjects();

        if ( reverse )
        {
            sorted = sorted.Reverse();
        }

        return sorted.Select( solution.GetProject ).Where( p => p != null ).Select( p => p! ).ToImmutableArray();
    }

    /// <summary>
    /// Returns the named projects first, in the order named, then the remaining projects in solution order.
    /// </summary>
    /// <remarks>
    /// A name that matches no project is ignored rather than an error, so that one order can be applied to several
    /// solutions, which is what the permutation runner does.
    /// </remarks>
    private static ImmutableArray<Project> OrderByName( Solution solution, ImmutableArray<string> order )
    {
        var projects = solution.Projects.ToList();
        var byName = projects.ToLookup( p => p.Name, StringComparer.Ordinal );
        var ordered = new List<Project>();
        var seen = new HashSet<ProjectId>();

        foreach ( var name in order )
        {
            foreach ( var project in byName[name] )
            {
                if ( seen.Add( project.Id ) )
                {
                    ordered.Add( project );
                }
            }
        }

        ordered.AddRange( projects.Where( p => !seen.Contains( p.Id ) ) );

        return ordered.ToImmutableArray();
    }
}
