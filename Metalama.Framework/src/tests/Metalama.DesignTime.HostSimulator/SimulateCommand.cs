// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.DesignTime.HostSimulator;

/// <summary>
/// Simulates the design-time behaviour of an IDE over a solution.
/// </summary>
[UsedImplicitly]
internal sealed class SimulateCommand : AsyncCommand<SimulateCommandSettings>
{
    /// <summary>
    /// The exit code returned when at least one infrastructure failure was observed.
    /// </summary>
    private const int _failureExitCode = 1;

    protected override async Task<int> ExecuteAsync( CommandContext context, SimulateCommandSettings settings, CancellationToken cancellationToken )
    {
        if ( settings.UseMSBuildLocator )
        {
            MSBuildEnvironment.Register();
        }

        // A design-time defect can be a deadlock as easily as an exception, so an unbounded run would turn a
        // reportable failure into a hung build. The timeout is enforced by racing the work against a delay rather
        // than by a cancellation token alone, because a deadlocked call never observes the token.
        var work = settings.AllPermutations
            ? RunAllPermutationsAsync( settings, cancellationToken )
            : RunOnceAsync( settings, cancellationToken );

        if ( settings.TimeoutSeconds > 0 )
        {
            var completed = await Task.WhenAny( work, Task.Delay( TimeSpan.FromSeconds( settings.TimeoutSeconds ), cancellationToken ) );

            if ( completed != work )
            {
                // Written in the canonical MSBuild format, so that the build engineering sees it as a diagnostic.
                Console.Out.WriteLine(
                    $"{Path.GetFileName( settings.FullSolutionPath )}: error SIM0002: "
                    + $"The simulation did not complete within {settings.TimeoutSeconds} seconds." );

                Console.Out.Flush();
                AnsiConsole.MarkupLine( "[red]Result: TIMED OUT.[/]" );

                // The process is left with threads blocked in the deadlock, so it cannot be shut down gracefully.
                Environment.Exit( _failureExitCode );
            }
        }

        try
        {
            return await work;
        }
        catch ( OperationCanceledException )
        {
            AnsiConsole.MarkupLine( "[yellow]Cancelled.[/]" );

            return 130;
        }
    }

    private static async Task<int> RunOnceAsync( SimulateCommandSettings settings, CancellationToken cancellationToken )
    {
        using var host = new DesignTimeHost( settings );

        AnsiConsole.MarkupLineInterpolated( $"Simulating [bold]{Path.GetFileName( settings.FullSolutionPath )}[/]." );

        var report = await AnsiConsole.Status()
            .Spinner( Spinner.Known.Dots )
            .StartAsync( "Loading the solution and running the pipelines...", _ => host.RunAsync( cancellationToken ) );

        report.Render( settings.Verbose );

        if ( report.HasFailure )
        {
            AnsiConsole.MarkupLine( "[red]Result: FAILED.[/]" );

            return _failureExitCode;
        }

        AnsiConsole.MarkupLine( "[green]Result: succeeded.[/]" );

        return 0;
    }

    /// <summary>
    /// Runs every permutation of the project order, each in a fresh child process.
    /// </summary>
    /// <remarks>
    /// A fresh process per permutation is not a convenience. Analyzer assemblies load into non-collectible load
    /// contexts, as they do in an IDE, so a second permutation in the same process would inherit the assemblies and
    /// the caches of the first one and would no longer be the scenario it claims to be.
    /// </remarks>
    private static async Task<int> RunAllPermutationsAsync( SimulateCommandSettings settings, CancellationToken cancellationToken )
    {
        ImmutableArray<string> projectNames;

        using ( var host = new DesignTimeHost( settings ) )
        {
            projectNames = await host.GetProjectNamesAsync( cancellationToken );
        }

        if ( projectNames.Length > 7 )
        {
            AnsiConsole.MarkupLineInterpolated(
                $"[red]The solution has {projectNames.Length} projects, which is {Factorial( projectNames.Length )} permutations. Use --order instead.[/]" );

            return 2;
        }

        var permutations = Permute( projectNames.ToList() ).ToList();
        AnsiConsole.MarkupLineInterpolated( $"Simulating {permutations.Count} permutation(s) of {projectNames.Length} project(s)." );

        var failed = new List<string>();

        foreach ( var permutation in permutations )
        {
            cancellationToken.ThrowIfCancellationRequested();

            var order = string.Join( ",", permutation );
            var exitCode = await RunChildProcessAsync( settings, order, cancellationToken );

            if ( exitCode == 0 )
            {
                AnsiConsole.MarkupLineInterpolated( $"  [green]ok[/]     {order}" );
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated( $"  [red]FAILED[/] {order}" );
                failed.Add( order );
            }
        }

        if ( failed.Count > 0 )
        {
            AnsiConsole.MarkupLineInterpolated( $"[red]Result: {failed.Count} of {permutations.Count} permutation(s) FAILED.[/]" );
            AnsiConsole.MarkupLineInterpolated( $"Re-run a failing one with: --order {failed[0]}" );

            return _failureExitCode;
        }

        AnsiConsole.MarkupLineInterpolated( $"[green]Result: all {permutations.Count} permutation(s) succeeded.[/]" );

        return 0;
    }

    private static async Task<int> RunChildProcessAsync( SimulateCommandSettings settings, string order, CancellationToken cancellationToken )
    {
        var startInfo = new ProcessStartInfo( Environment.ProcessPath! ) { UseShellExecute = false };

        // When the host runs as a framework-dependent application, ProcessPath is the dotnet muxer and the assembly
        // must be passed explicitly.
        var entryAssembly = Environment.GetCommandLineArgs()[0];

        if ( !string.Equals(
                Path.GetFileNameWithoutExtension( Environment.ProcessPath ),
                Path.GetFileNameWithoutExtension( entryAssembly ),
                StringComparison.OrdinalIgnoreCase ) )
        {
            startInfo.ArgumentList.Add( entryAssembly );
        }

        startInfo.ArgumentList.Add( settings.FullSolutionPath );
        startInfo.ArgumentList.Add( "--order" );
        startInfo.ArgumentList.Add( order );

        if ( settings.Verbose )
        {
            startInfo.ArgumentList.Add( "--verbose" );
        }

        if ( settings.UseMSBuildLocator )
        {
            startInfo.ArgumentList.Add( "--msbuild-locator" );
        }

        foreach ( var property in settings.Properties )
        {
            startInfo.ArgumentList.Add( "--property" );
            startInfo.ArgumentList.Add( property );
        }

        using var process = Process.Start( startInfo )
                            ?? throw new InvalidOperationException( "Cannot start the child process." );

        await process.WaitForExitAsync( cancellationToken );

        return process.ExitCode;
    }

    private static IEnumerable<IReadOnlyList<string>> Permute( List<string> items )
    {
        if ( items.Count <= 1 )
        {
            yield return items;

            yield break;
        }

        for ( var i = 0; i < items.Count; i++ )
        {
            var rest = new List<string>( items );
            rest.RemoveAt( i );

            foreach ( var permutation in Permute( rest ) )
            {
                yield return new[] { items[i] }.Concat( permutation ).ToList();
            }
        }
    }

    private static int Factorial( int n )
    {
        var result = 1;

        for ( var i = 2; i <= n; i++ )
        {
            result *= i;
        }

        return result;
    }
}
