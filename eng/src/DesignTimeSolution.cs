// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Solutions;
using PostSharp.Engineering.BuildTools.Utilities;
using System;
using System.IO;

namespace BuildMetalama;

/// <summary>
/// A <see cref="TestableSolution"/> that exercises a solution at design time, with
/// <c>Metalama.DesignTime.HostSimulator</c>, instead of building it with <c>dotnet build</c>.
/// </summary>
/// <remarks>
/// <para>
/// A batch build gives one compiler process per project and no shared state between them, so it cannot reach the
/// code paths that only run in an IDE: the pipeline caches that one project's analysis serves to another, the
/// per-document analyzer requests, and the loading of several Metalama versions into one process. The scenarios
/// under the design-time directory exist for those paths, and this class is what runs them.
/// </para>
/// <para>
/// The scenario is only restored, never built, because a scenario is allowed to be one that does not compile. What
/// the simulator needs is the restore output, since the workspace evaluates the projects rather than compiling
/// them.
/// </para>
/// <para>
/// Assertions come from <c>test.json</c> exactly as they do for a <see cref="DotNetSolution"/>: the simulator
/// writes its diagnostics in the canonical MSBuild format, so
/// <see cref="TestableSolution"/> can match them without knowing which engine produced them.
/// </para>
/// </remarks>
internal sealed class DesignTimeSolution : TestableSolution
{
    /// <summary>
    /// The target framework of the simulator that is invoked. The simulator is multi-targeted so that its MSBuild
    /// matches the .NET SDK, and the build engineering runs on the latest.
    /// </summary>
    private const string _simulatorTargetFramework = "net9.0";

    private const string _simulatorProjectDirectory = @"Metalama.Framework\src\tests\Metalama.DesignTime.HostSimulator";

    public DesignTimeSolution( string solutionPath ) : base( solutionPath ) { }

    /// <summary>
    /// Gets the traversal order passed to the simulator, or <c>null</c> to use the solution order.
    /// </summary>
    public string? Traversal { get; init; }

    /// <summary>
    /// Gets a value indicating whether the simulator must try every order of the projects.
    /// </summary>
    public bool AllPermutations { get; init; }

    public override bool Pack( BuildContext context, BuildSettings settings ) => throw new NotSupportedException();

    public override bool Restore( BuildContext context, BuildSettings settings )
        => DotNetHelper.Run(
            context,
            settings,
            this.GetFinalSolutionPath( context ),
            "restore",
            "--no-cache",
            false,
            this.CreateInvocationOptions() );

    /// <summary>
    /// Returns the path of the simulator assembly, which <see cref="ManyDesignTimeSolutions"/> has built before any
    /// scenario runs.
    /// </summary>
    public static string GetSimulatorPath( BuildContext context, BuildSettings settings )
    {
        var configuration = context.Product.DependencyDefinition.MSBuildConfiguration[settings.BuildConfiguration];

        return Path.Combine(
            context.RepoDirectory,
            _simulatorProjectDirectory,
            "bin",
            configuration,
            _simulatorTargetFramework,
            "Metalama.DesignTime.HostSimulator.dll" );
    }

    public static string GetSimulatorProjectPath( BuildContext context )
        => Path.Combine( context.RepoDirectory, _simulatorProjectDirectory, "Metalama.DesignTime.HostSimulator.csproj" );

    protected override bool Invoke(
        BuildContext context,
        BuildSettings settings,
        SolutionCommand command,
        EffectiveTestOptions options,
        string logName,
        bool captureOutput,
        out int exitCode,
        out string output )
    {
        var simulator = GetSimulatorPath( context, settings );

        if ( !File.Exists( simulator ) )
        {
            context.Console.WriteError(
                $"The design-time host simulator was not found at '{simulator}'. If the target framework of "
                + $"'{_simulatorProjectDirectory}' no longer includes '{_simulatorTargetFramework}', update "
                + $"{nameof(DesignTimeSolution)}.{nameof(_simulatorTargetFramework)}." );

            exitCode = 1;
            output = "";

            return false;
        }

        // A scenario can reference an assembly by file path rather than by project reference, which is the only way
        // to express some of the reference graphs these scenarios exist for. Those files have to exist before the
        // workspace evaluates the projects, so the scenario is built first.
        //
        // The result is deliberately ignored: a design-time scenario is allowed to be one that does not compile,
        // which is exactly what the scenario for #1749 is. What matters is the output that a partial build produces,
        // not its exit code.
        _ = DotNetHelper.Run(
            context,
            settings,
            this.GetFinalSolutionPath( context ),
            "build",
            // Each project is compiled in its own process. Loading two versions of one compile-time assembly into a
            // shared compiler server fails on the second one, which would stop the scenario before it produces the
            // assemblies the simulator needs.
            "-p:UseSharedCompilation=false",
            true,
            out _,
            out _,
            this.CreateInvocationOptions(),
            logName + ".prepare" );

        var commandLine = $"\"{simulator}\" \"{this.GetFinalSolutionPath( context )}\"";

        if ( this.AllPermutations )
        {
            commandLine += " --permutations";
        }
        else if ( this.Traversal != null )
        {
            commandLine += $" --traversal {this.Traversal}";
        }

        var workingDirectory = context.GetWorkingDirectory( this.GetFinalSolutionPath( context ) );

        if ( !captureOutput )
        {
            exitCode = 0;
            output = "";

            return ToolInvocationHelper.InvokeTool( context.Console, "dotnet", commandLine, workingDirectory, this.CreateInvocationOptions() );
        }

        return ToolInvocationHelper.InvokeTool(
            context.Console,
            "dotnet",
            commandLine,
            workingDirectory,
            out exitCode,
            out output,
            this.CreateInvocationOptions() );
    }
}
