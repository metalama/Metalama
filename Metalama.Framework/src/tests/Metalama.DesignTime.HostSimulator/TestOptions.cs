// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Spectre.Console;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Metalama.DesignTime.HostSimulator;

/// <summary>
/// The assertions of the <c>designtime-test.json</c> file that sits beside a scenario's solution.
/// </summary>
/// <remarks>
/// <para>
/// This is deliberately not the <c>test.json</c> that the build engineering reads. That file belongs to the
/// engineering, which applies its regular expressions to the whole output of this process, including the trace and
/// the output of the tools the analysis starts. This class sees only the diagnostics the simulation reported, so the
/// same regular expression can hold for one and not for the other. Two files that are read by one owner each cannot
/// disagree about what they mean.
/// </para>
/// <para>
/// A scenario that only needs the engineering to judge it has no <c>designtime-test.json</c> at all, and nothing
/// here applies to it.
/// </para>
/// </remarks>
internal sealed class TestOptions
{
    /// <summary>
    /// Gets the regular expressions of which each must match at least one reported line.
    /// </summary>
    [JsonPropertyName( "ExpectedDiagnosticsRegexes" )]
    public string[] ExpectedDiagnosticsRegexes { get; init; } = [];

    /// <summary>
    /// Gets the regular expressions of which none may match any reported line.
    /// </summary>
    [JsonPropertyName( "ForbiddenDiagnosticsRegexes" )]
    public string[] ForbiddenDiagnosticsRegexes { get; init; } = [];

    /// <summary>
    /// Gets the regular expressions of which each must match at least one reported line. This is the member a
    /// scenario uses when the outcome it asserts is a failure.
    /// </summary>
    [JsonPropertyName( "ErrorRegexes" )]
    public string[] ErrorRegexes { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether this file asserts anything about the reported diagnostics.
    /// </summary>
    public bool HasAssertions
        => this.ExpectedDiagnosticsRegexes.Length > 0 || this.ForbiddenDiagnosticsRegexes.Length > 0 || this.ErrorRegexes.Length > 0;

    /// <summary>
    /// The name of the file this class reads.
    /// </summary>
    public const string FileName = "designtime-test.json";

    /// <summary>
    /// Reads the <see cref="FileName"/> beside <paramref name="solutionPath"/>, or returns <c>null</c> when there is
    /// none.
    /// </summary>
    public static TestOptions? TryLoad( string solutionPath )
    {
        var path = Path.Combine( Path.GetDirectoryName( solutionPath )!, FileName );

        if ( !File.Exists( path ) )
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TestOptions>(
                File.ReadAllText( path ),
                new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true } );
        }
        catch ( JsonException exception )
        {
            AnsiConsole.MarkupLineInterpolated( $"[red]Cannot read '{path}': {exception.Message}[/]" );

            return null;
        }
    }

    /// <summary>
    /// Evaluates the assertions against the reported lines, writing every unsatisfied one, and returns whether they
    /// all hold.
    /// </summary>
    public bool Evaluate( ImmutableArray<string> reportedLines )
    {
        var satisfied = true;

        foreach ( var pattern in this.ExpectedDiagnosticsRegexes.Concat( this.ErrorRegexes ) )
        {
            if ( !reportedLines.Any( line => Regex.IsMatch( line, pattern ) ) )
            {
                AnsiConsole.MarkupLineInterpolated( $"[red]Expected a diagnostic matching '{pattern}', but none was reported.[/]" );
                satisfied = false;
            }
        }

        foreach ( var pattern in this.ForbiddenDiagnosticsRegexes )
        {
            var forbidden = reportedLines.FirstOrDefault( line => Regex.IsMatch( line, pattern ) );

            if ( forbidden != null )
            {
                AnsiConsole.MarkupLineInterpolated( $"[red]Forbidden diagnostic matching '{pattern}' was reported: {forbidden}[/]" );
                satisfied = false;
            }
        }

        return satisfied;
    }
}
