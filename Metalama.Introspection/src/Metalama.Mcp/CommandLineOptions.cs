// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Globalization;
using System.IO;

namespace Metalama.Mcp;

/// <summary>
/// The command line of <c>metalama-mcp</c>. There is deliberately no option for a network transport.
/// </summary>
internal sealed class CommandLineOptions
{
    public const string WorkerOption = "--worker";
    public const string OutputDirectoryOption = "--output-dir";
    public const string TimeoutOption = "--timeout";

    /// <summary>
    /// Gets the directory where HTML results are written.
    /// </summary>
    public string OutputDirectory { get; private set; } = Path.Combine( Path.GetTempPath(), "metalama-mcp" );

    /// <summary>
    /// Gets the default soft timeout of a query. The hard timeout, after which the worker is killed, is twice as long.
    /// </summary>
    public TimeSpan DefaultTimeout { get; private set; } = TimeSpan.FromSeconds( 60 );

    public string? WorkerInputHandle { get; private set; }

    public string? WorkerOutputHandle { get; private set; }

    public string? Error { get; private set; }

    public static CommandLineOptions Parse( string[] args )
    {
        var options = new CommandLineOptions();

        for ( var i = 0; i < args.Length; i++ )
        {
            switch ( args[i] )
            {
                case WorkerOption when i + 2 < args.Length:
                    options.WorkerInputHandle = args[++i];
                    options.WorkerOutputHandle = args[++i];

                    break;

                case OutputDirectoryOption when i + 1 < args.Length:
                    options.OutputDirectory = Path.GetFullPath( args[++i] );

                    break;

                case TimeoutOption when i + 1 < args.Length && int.TryParse( args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds )
                                                             && seconds > 0:
                    options.DefaultTimeout = TimeSpan.FromSeconds( seconds );
                    i++;

                    break;

                case "--help" or "-h":
                    options.Error =
                        $"Usage: metalama-mcp [{OutputDirectoryOption} <directory>] [{TimeoutOption} <seconds>]\n\nA local MCP server that communicates over stdio only. Register it with an MCP client, for example:\n  claude mcp add metalama -- dnx Metalama.Mcp";

                    break;

                default:
                    options.Error = $"Unknown or incomplete argument '{args[i]}'. metalama-mcp communicates over stdio only and has no network option. Use --help.";

                    break;
            }

            if ( options.Error != null )
            {
                break;
            }
        }

        return options;
    }
}
