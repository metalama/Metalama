// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.DesignTime.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.DesignTime.HostSimulator;

/// <summary>
/// Opens a solution with an <see cref="MSBuildWorkspace"/>, evaluated the way an IDE evaluates it, and analyzes it with a
/// <see cref="DesignTimeHost"/>.
/// </summary>
internal sealed class SolutionSimulator : IDisposable
{
    private readonly SimulateCommandSettings _settings;
    private readonly MSBuildWorkspace _workspace;
    private Solution? _solution;

    public SolutionSimulator( SimulateCommandSettings settings )
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
        var solution = await this.GetSolutionAsync( cancellationToken );

        var host = new DesignTimeHost(
            new DesignTimeHostOptions { Traversal = this._settings.Traversal, OrderedProjectNames = this._settings.OrderedProjectNames } );

        var report = this._settings.VisualStudio
            ? await host.RunVisualStudioInProcessAsync( this._workspace, cancellationToken )
            : await host.RunAsync( solution, cancellationToken );

        foreach ( var failure in this._workspace.Diagnostics.Where( d => d.Kind == WorkspaceDiagnosticKind.Failure ) )
        {
            report.GetOrAddProject( "<workspace>" ).AddError( failure.Message );
        }

        return report;
    }

    public void Dispose() => this._workspace.Dispose();
}
