// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Mcp.Supervisor;
using Metalama.Mcp.Worker;
using System;
using System.Threading.Tasks;

namespace Metalama.Mcp;

/// <summary>
/// The entry point. Without arguments, the process is the supervisor, which speaks MCP on stdio. With <c>--worker</c>, it is
/// the worker, which the supervisor starts and talks to over anonymous pipes.
/// </summary>
/// <remarks>
/// The supervisor never touches the code model, Roslyn or MSBuild, so that none of these assemblies is loaded into the
/// process that holds the MCP connection. All of them run in the worker.
/// </remarks>
internal static class Program
{
    public static async Task<int> Main( string[] args )
    {
        var options = CommandLineOptions.Parse( args );

        if ( options.Error != null )
        {
            await Console.Error.WriteLineAsync( options.Error );

            return 2;
        }

        if ( options.WorkerInputHandle != null && options.WorkerOutputHandle != null )
        {
            return await WorkerHost.RunAsync( options );
        }

        return await SupervisorHost.RunAsync( options );
    }
}
