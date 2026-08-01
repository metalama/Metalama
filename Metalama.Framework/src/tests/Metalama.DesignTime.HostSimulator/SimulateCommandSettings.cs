// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Metalama.DesignTime.HostSimulator;

/// <summary>
/// The command line of the simulator.
/// </summary>
internal sealed class SimulateCommandSettings : CommandSettings
{
    [UsedImplicitly]
    [Description( "The path of the solution to simulate." )]
    [CommandArgument( 0, "<solution>" )]
    public string SolutionPath { get; init; } = null!;

    [UsedImplicitly]
    [Description(
        "Analyze the named projects in this order, as a comma-separated list. Defaults to the solution order. "
        + "The order matters: a design-time pipeline caches its configuration and can serve it to the pipeline of a "
        + "dependent project, so a defect can depend on which project is analyzed first." )]
    [CommandOption( "--order <PROJECTS>" )]
    public string? Order { get; init; }

    [UsedImplicitly]
    [Description(
        "The order in which projects are analyzed when --order is not given. 'Solution' is the order in which the "
        + "solution declares them, 'Graph' is the dependency order (a project after the ones it references), and "
        + "'Reverse' is the opposite (a project before the ones it references). 'Reverse' is the interesting one: an "
        + "editor routinely analyzes a dependent project before its dependency, which is when a downstream pipeline "
        + "asks for an upstream configuration that does not exist yet." )]
    [CommandOption( "--traversal <ORDER>" )]
    [DefaultValue( TraversalOrder.Solution )]
    public TraversalOrder Traversal { get; init; }

    [UsedImplicitly]
    [Description( "Simulate every order of the projects, each in a fresh process." )]
    [CommandOption( "--permutations" )]
    public bool AllPermutations { get; init; }

    [UsedImplicitly]
    [Description( "An MSBuild property, as Name=Value. Can be repeated." )]
    [CommandOption( "-p|--property <NAME=VALUE>" )]
    public string[] Properties { get; init; } = [];

    [UsedImplicitly]
    [Description(
        "Register an MSBuild instance with Microsoft.Build.Locator. Off by default, because the workspace uses an "
        + "out-of-process build host that locates MSBuild itself, and registering the locator prevents it from starting." )]
    [CommandOption( "--msbuild-locator" )]
    public bool UseMSBuildLocator { get; init; }

    [UsedImplicitly]
    [Description(
        "The Metalama trace categories to log, as a comma-separated list, or '*' for all of them. Errors, warnings "
        + "and infos are logged whatever this is set to. The default names no real category, so no trace is logged." )]
    [CommandOption( "--trace <CATEGORIES>" )]
    [DefaultValue( NoTraceCategory )]
    public string TraceCategories { get; init; } = NoTraceCategory;

    /// <summary>
    /// The value given to <c>METALAMA_CONSOLE_TRACE</c> when no trace category is requested.
    /// </summary>
    /// <remarks>
    /// The variable selects the console logger by being non-empty, and its value is a category filter, so it cannot
    /// be left empty to mean "no trace". This names no category that any Metalama component logs under, which
    /// yields errors, warnings and infos on the console and no trace.
    /// </remarks>
    public const string NoTraceCategory = "none";

    [UsedImplicitly]
    [Description(
        "Abandon the simulation after this many seconds and report a failure. A design-time defect can be a "
        + "deadlock as easily as an exception, and an unbounded run turns that into a hung build instead of a "
        + "reported failure. Zero disables the timeout." )]
    [CommandOption( "--timeout <SECONDS>" )]
    [DefaultValue( 600 )]
    public int TimeoutSeconds { get; init; }

    [UsedImplicitly]
    [Description( "Print every diagnostic, not only the ones that indicate a failure." )]
    [CommandOption( "-v|--verbose" )]
    public bool Verbose { get; init; }

    /// <summary>
    /// Gets the requested project order, or an empty array to use the solution order.
    /// </summary>
    public ImmutableArray<string> OrderedProjectNames
        => this.Order == null
            ? ImmutableArray<string>.Empty
            : this.Order.Split( ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries ).ToImmutableArray();

    /// <summary>
    /// Gets the MSBuild properties, parsed from <see cref="Properties"/>.
    /// </summary>
    public ImmutableDictionary<string, string> ParsedProperties
        => this.Properties
            .Select( p => p.Split( '=', 2 ) )
            .ToImmutableDictionary( p => p[0], p => p[1], StringComparer.Ordinal );

    public string FullSolutionPath => Path.GetFullPath( this.SolutionPath );

    public override ValidationResult Validate()
    {
        if ( !File.Exists( this.FullSolutionPath ) )
        {
            return ValidationResult.Error( $"The solution '{this.FullSolutionPath}' does not exist." );
        }

        if ( this.AllPermutations && this.Order != null )
        {
            return ValidationResult.Error( "--permutations and --order are mutually exclusive." );
        }

        if ( this.Order != null && this.Traversal != TraversalOrder.Solution )
        {
            return ValidationResult.Error( "--order and --traversal are mutually exclusive." );
        }

        if ( this.AllPermutations && this.Traversal != TraversalOrder.Solution )
        {
            return ValidationResult.Error( "--permutations and --traversal are mutually exclusive." );
        }

        var malformedProperty = this.Properties.FirstOrDefault( p => !p.Contains( '=', StringComparison.Ordinal ) );

        if ( malformedProperty != null )
        {
            return ValidationResult.Error( $"The property '{malformedProperty}' is not of the form Name=Value." );
        }

        return ValidationResult.Success();
    }
}
