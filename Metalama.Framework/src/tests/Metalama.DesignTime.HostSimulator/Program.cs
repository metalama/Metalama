// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Spectre.Console.Cli;
using System.Threading.Tasks;

namespace Metalama.DesignTime.HostSimulator;

/// <summary>
/// Entry point of the design-time host simulator.
/// </summary>
internal static class Program
{
    private static Task<int> Main( string[] args )
    {
        var app = new CommandApp<SimulateCommand>();

        app.Configure(
            config =>
            {
                config.SetApplicationName( "Metalama.DesignTime.HostSimulator" );

                config.SetExceptionHandler( ( exception, _ ) =>
                {
                    Spectre.Console.AnsiConsole.WriteException( exception );

                    return 3;
                } );

                config.AddExample( "MySolution.sln" );
                config.AddExample( "MySolution.sln", "--traversal", "Reverse" );
                config.AddExample( "MySolution.sln", "--permutations" );
            } );

        return app.RunAsync( args );
    }
}
