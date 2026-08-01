// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.DesignTime.HostSimulator;

/// <summary>
/// Loads a solution into a Roslyn workspace and analyzes its projects in a given order, the way an IDE does.
/// </summary>
internal sealed class DesignTimeHost : IDisposable
{
    private readonly SimulateCommandSettings _settings;
    private readonly MSBuildWorkspace _workspace;
    private Solution? _solution;

    public DesignTimeHost( SimulateCommandSettings settings )
    {
        this._settings = settings;

        var properties = settings.ParsedProperties.ToDictionary( p => p.Key, p => p.Value, StringComparer.Ordinal );

        // An IDE evaluates projects with the design-time targets, which is what makes the compiler-visible
        // properties and the analyzer references available without a build having run.
        properties.TryAdd( "DesignTimeBuild", "true" );
        properties.TryAdd( "BuildingInsideVisualStudio", "true" );
        properties.TryAdd( "BuildingProject", "false" );

        // MSBuildWorkspace evaluates each project on its own, so it does not define the solution properties that an IDE
        // defines. They are supplied here because Metalama.Framework.targets derives the project discriminator symbol
        // from a path relative to the solution directory, and without them every project would fall back to the path
        // that has no directory, which is a weaker identifier than the one a real host uses.
        properties.TryAdd( "SolutionDir", Path.GetDirectoryName( settings.FullSolutionPath ) + Path.DirectorySeparatorChar );
        properties.TryAdd( "SolutionPath", settings.FullSolutionPath );
        properties.TryAdd( "SolutionFileName", Path.GetFileName( settings.FullSolutionPath ) );
        properties.TryAdd( "SolutionName", Path.GetFileNameWithoutExtension( settings.FullSolutionPath ) );

        this._workspace = MSBuildWorkspace.Create( properties );
        this._workspace.SkipUnrecognizedProjects = true;
    }

    private async Task<Solution> GetSolutionAsync( CancellationToken cancellationToken )
        => this._solution ??= await this._workspace.OpenSolutionAsync( this._settings.FullSolutionPath, cancellationToken: cancellationToken );

    /// <summary>
    /// Returns the names of the projects of the solution, without analyzing them.
    /// </summary>
    public async Task<ImmutableArray<string>> GetProjectNamesAsync( CancellationToken cancellationToken )
    {
        var solution = await this.GetSolutionAsync( cancellationToken );

        return solution.Projects.Select( p => p.Name ).Distinct( StringComparer.Ordinal ).ToImmutableArray();
    }

    public async Task<SimulationReport> RunAsync( CancellationToken cancellationToken )
    {
        var report = new SimulationReport();

        var solution = await this.GetSolutionAsync( cancellationToken );

        foreach ( var failure in this._workspace.Diagnostics.Where( d => d.Kind == WorkspaceDiagnosticKind.Failure ) )
        {
            report.GetOrAddProject( "<workspace>" ).AddError( failure.Message );
        }

        var loader = new IsolatedAnalyzerAssemblyLoader();

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
    /// Returns the projects to analyze, in the requested order.
    /// </summary>
    private ImmutableArray<Project> OrderProjects( Solution solution )
    {
        var explicitOrder = this._settings.OrderedProjectNames;

        if ( !explicitOrder.IsEmpty )
        {
            return OrderByName( solution, explicitOrder );
        }

        return this._settings.Traversal switch
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
    /// <see cref="ProjectDependencyGraph.GetTopologicallySortedProjects"/> returns the projects so that a project
    /// comes after those it references, which is the order a batch build uses. Reversing it produces the order an
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

    public void Dispose() => this._workspace.Dispose();
}
