// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.DesignTime.Hosting;
using Spectre.Console;
using System;
using System.Collections.Immutable;
using System.Globalization;

namespace Metalama.DesignTime.HostSimulator;

/// <summary>
/// Writes a <see cref="SimulationReport"/> to the console.
/// </summary>
internal static class ReportRenderer
{
    /// <summary>
    /// Writes a summary table, then every failure and, when <paramref name="verbose"/> is <c>true</c>, every diagnostic.
    /// Returns the diagnostic lines that were written.
    /// </summary>
    /// <remarks>
    /// The diagnostic lines are written straight to <see cref="Console.Out"/> rather than through <see cref="AnsiConsole"/>,
    /// because Spectre wraps its output to the console width, and a wrapped diagnostic is no longer one line. The build
    /// engineering asserts on these lines with the regular expressions of a <c>test.json</c> file, and it splits the output
    /// by line, so a wrapped line would silently fail to match.
    /// </remarks>
    public static ImmutableArray<string> Render( SimulationReport report, bool verbose )
    {
        if ( !report.ProjectOrder.IsEmpty )
        {
            AnsiConsole.MarkupLineInterpolated( $"Order: {string.Join( " -> ", report.ProjectOrder )}" );
        }

        AnsiConsole.MarkupLineInterpolated( $"Analyzer load contexts: {report.LoadContextCount}" );

        var table = new Table().Border( TableBorder.Rounded );
        table.AddColumn( "Project" );
        table.AddColumn( new TableColumn( "Analyzers" ).RightAligned() );
        table.AddColumn( new TableColumn( "Generators" ).RightAligned() );
        table.AddColumn( new TableColumn( "Generated" ).RightAligned() );
        table.AddColumn( new TableColumn( "Diagnostics" ).RightAligned() );
        table.AddColumn( "Status" );

        foreach ( var project in report.Projects )
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

        var lines = report.GetReportedLines( verbose );

        foreach ( var line in lines )
        {
            Console.Out.WriteLine( line );
        }

        return lines;
    }
}
