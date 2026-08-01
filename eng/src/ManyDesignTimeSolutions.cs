// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Solutions;
using PostSharp.Engineering.BuildTools.Utilities;

namespace BuildMetalama;

/// <summary>
/// An implementation of <see cref="ManySolutions"/> that exercises each scenario at design time, with
/// <c>Metalama.DesignTime.HostSimulator</c>, instead of building it with <c>dotnet build</c>.
/// </summary>
/// <remarks>
/// This is the design-time counterpart of <see cref="ManyDotNetSolutions"/>. Discovery, scheduling and reporting
/// are inherited unchanged; only the engine that runs a scenario differs.
/// </remarks>
internal sealed class ManyDesignTimeSolutions : ManySolutions
{
    /// <param name="directory">A directory, relative to the root of the repository.</param>
    public ManyDesignTimeSolutions( string directory ) : base( directory ) { }

    /// <summary>
    /// Gets the traversal order passed to the simulator for every scenario, or <c>null</c> to use the solution order.
    /// </summary>
    public string? Traversal { get; init; }

    /// <summary>
    /// Gets a value indicating whether the simulator must try every order of the projects of every scenario.
    /// </summary>
    public bool AllPermutations { get; init; }

    protected override TestableSolution CreateSolution( string projectPath, BuildMethod testMethod )
        => new DesignTimeSolution( projectPath )
        {
            EnvironmentVariables = this.EnvironmentVariables,
            TestMethod = testMethod,
            Traversal = this.Traversal,
            AllPermutations = this.AllPermutations
        };

    public override bool Build( BuildContext context, BuildSettings settings )
        => this.BuildSimulator( context, settings ) && base.Build( context, settings );

    public override bool Test( BuildContext context, BuildSettings settings )
        => this.BuildSimulator( context, settings ) && base.Test( context, settings );

    /// <summary>
    /// Builds the simulator once, before any scenario runs.
    /// </summary>
    /// <remarks>
    /// The scenarios run concurrently, so each of them building the simulator on demand would have them race on the
    /// same output directory. Building it once here keeps the scenario invocation to a plain <c>dotnet &lt;dll&gt;</c>.
    /// </remarks>
    private bool BuildSimulator( BuildContext context, BuildSettings settings )
    {
        var simulatorProject = DesignTimeSolution.GetSimulatorProjectPath( context );

        context.Console.WriteMessage( $"Building the design-time host simulator '{simulatorProject}'." );

        return DotNetHelper.Run(
            context,
            settings,
            simulatorProject,
            "build",
            "",
            true,
            this.CreateInvocationOptions() );
    }

    private ToolInvocationOptions CreateInvocationOptions() => new( this.EnvironmentVariables );
}
