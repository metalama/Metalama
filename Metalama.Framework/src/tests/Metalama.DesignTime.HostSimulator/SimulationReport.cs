// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace Metalama.DesignTime.HostSimulator;

/// <summary>
/// What one project produced during a simulation.
/// </summary>
internal sealed class ProjectReport
{
    private readonly List<Diagnostic> _diagnostics = new();
    private readonly List<string> _errors = new();

    public ProjectReport( string projectName )
    {
        this.ProjectName = projectName;
    }

    public string ProjectName { get; }

    public int AnalyzerCount { get; set; }

    public int GeneratorCount { get; set; }

    public int GeneratedDocumentCount { get; set; }

    /// <summary>
    /// Gets the failures that are not expressed as diagnostics, such as an analyzer that threw or an analyzer
    /// assembly that could not be loaded.
    /// </summary>
    public IReadOnlyList<string> Errors => this._errors;

    public IReadOnlyList<Diagnostic> Diagnostics => this._diagnostics;

    public void AddDiagnostics( IEnumerable<Diagnostic> diagnostics ) => this._diagnostics.AddRange( diagnostics );

    public void AddError( string error ) => this._errors.Add( error );

    /// <summary>
    /// Gets the diagnostics that mean the analyzer itself failed, as opposed to diagnostics about the user's code.
    /// </summary>
    /// <remarks>
    /// <c>AD0001</c> is Roslyn's report of an analyzer that threw, <c>CS8785</c> its report of a source generator
    /// that threw, and <c>LAMA0001</c> Metalama's report of an unhandled exception. All three are what this
    /// simulator exists to catch, and all three are warnings rather than errors, so severity alone does not
    /// identify them.
    /// </remarks>
    public IEnumerable<Diagnostic> InfrastructureFailures
        => this._diagnostics.Where(
            d => d.Id is "AD0001" or "CS8785" or "LAMA0001"
                 || (d.Severity == DiagnosticSeverity.Error && d.Id.StartsWith( "LAMA", StringComparison.Ordinal )) );

    public bool HasFailure => this._errors.Count > 0 || this.InfrastructureFailures.Any();
}

/// <summary>
/// What a whole simulation produced.
/// </summary>
internal sealed class SimulationReport
{
    private readonly Dictionary<string, ProjectReport> _projects = new( StringComparer.Ordinal );
    private readonly List<string> _projectOrder = new();

    public ProjectReport GetOrAddProject( string projectName )
    {
        if ( !this._projects.TryGetValue( projectName, out var report ) )
        {
            report = new ProjectReport( projectName );
            this._projects.Add( projectName, report );
            this._projectOrder.Add( projectName );
        }

        return report;
    }

    public ImmutableArray<ProjectReport> Projects
        => this._projectOrder.Select( name => this._projects[name] ).ToImmutableArray();

    public bool HasFailure => this.Projects.Any( p => p.HasFailure );

    /// <summary>
    /// Gets or sets the order in which the projects were analyzed.
    /// </summary>
    public ImmutableArray<string> ProjectOrder { get; set; } = ImmutableArray<string>.Empty;

    /// <summary>
    /// Gets or sets the number of analyzer load contexts that were created, which is the number of distinct
    /// analyzer directories, and therefore the number of distinct Metalama versions the solution loaded.
    /// </summary>
    public int LoadContextCount { get; set; }

    /// <summary>
    /// Renders the report, listing every failure and, when <paramref name="verbose"/> is set, every diagnostic.
    /// </summary>
    public void Render( bool verbose )
    {
        if ( !this.ProjectOrder.IsEmpty )
        {
            AnsiConsole.MarkupLineInterpolated( $"Order: {string.Join( " -> ", this.ProjectOrder )}" );
        }

        AnsiConsole.MarkupLineInterpolated( $"Analyzer load contexts: {this.LoadContextCount}" );

        var table = new Table().Border( TableBorder.Rounded );
        table.AddColumn( "Project" );
        table.AddColumn( new TableColumn( "Analyzers" ).RightAligned() );
        table.AddColumn( new TableColumn( "Generators" ).RightAligned() );
        table.AddColumn( new TableColumn( "Generated" ).RightAligned() );
        table.AddColumn( new TableColumn( "Diagnostics" ).RightAligned() );
        table.AddColumn( "Status" );

        foreach ( var project in this.Projects )
        {
            table.AddRow(
                Markup.Escape( project.ProjectName ),
                project.AnalyzerCount.ToString( CultureInfo.InvariantCulture ),
                project.GeneratorCount.ToString( CultureInfo.InvariantCulture ),
                project.GeneratedDocumentCount.ToString( CultureInfo.InvariantCulture ),
                project.Diagnostics.Count.ToString( CultureInfo.InvariantCulture ),
                project.HasFailure ? "[red]FAILED[/]" : "[green]ok[/]" );
        }

        AnsiConsole.Write( table );

        this.WriteDiagnostics( verbose );
    }

    /// <summary>
    /// Writes every diagnostic in the canonical MSBuild format, the way a build writes it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is written straight to <see cref="Console.Out"/> rather than through <see cref="AnsiConsole"/>, because
    /// Spectre wraps its output to the console width, and a wrapped diagnostic is no longer one line. The build
    /// engineering asserts on these lines with the regular expressions of a <c>test.json</c> file, and it splits the
    /// output by line, so a wrapped line would silently fail to match.
    /// </para>
    /// <para>
    /// <see cref="Diagnostic.ToString"/> already produces the canonical
    /// <c>path(line,column): severity ID: message</c> format, which is what makes a design-time scenario assertable
    /// with exactly the same <c>test.json</c> syntax as a compile-time one.
    /// </para>
    /// </remarks>
    private void WriteDiagnostics( bool verbose )
    {
        foreach ( var project in this.Projects )
        {
            // Failures that are not diagnostics still have to reach the assertion machinery, which only looks at
            // lines containing ': error ' or ': warning '.
            foreach ( var error in project.Errors )
            {
                Console.Out.WriteLine( $"{project.ProjectName}: error SIM0001: {SingleLine( error )}" );
            }

            foreach ( var diagnostic in project.Diagnostics )
            {
                if ( verbose || diagnostic.Severity >= DiagnosticSeverity.Warning )
                {
                    Console.Out.WriteLine( SingleLine( diagnostic.ToString() ) );
                }
            }
        }
    }

    /// <summary>
    /// Collapses a message onto a single line, so that a multi-line message such as a stack trace stays matchable.
    /// </summary>
    private static string SingleLine( string message )
        => message.Replace( "\r\n", " ", StringComparison.Ordinal ).Replace( '\n', ' ' ).Replace( '\r', ' ' );
}
