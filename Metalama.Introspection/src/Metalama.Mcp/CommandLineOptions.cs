// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Globalization;

namespace Metalama.Mcp;

/// <summary>
/// The command line of <c>metalama-mcp</c>. There is deliberately no option for a network transport of MCP, and no option to
/// choose the address of the HTTPS server that shows html results, which is always a random port on loopback.
/// </summary>
internal sealed class CommandLineOptions
{
    public const string WorkerOption = "--worker";
    public const string TimeoutOption = "--timeout";
    public const string EditorLinkOption = "--editor-link";
    public const string NoHttpOption = "--no-http";
    public const string DefaultEditorLink = "vscode://file{path}:{line}";

    /// <summary>
    /// Gets a value indicating whether the HTTPS server that shows html results is disabled. With it, the server never
    /// listens on any port.
    /// </summary>
    public bool NoHttp { get; private set; }

    /// <summary>
    /// Gets the template of the link with which an HTML page opens a source location in an editor, or <c>null</c> when
    /// locations are not links. <c>{path}</c> is the absolute path in URI form, such as <c>/C:/src/a.cs</c> or
    /// <c>/home/me/a.cs</c>, and <c>{line}</c> is the line number.
    /// </summary>
    public string? EditorLink { get; private set; } = DefaultEditorLink;

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

                case NoHttpOption:
                    options.NoHttp = true;

                    break;

                case EditorLinkOption when i + 1 < args.Length:
                    var editorLink = args[++i];
                    options.EditorLink = editorLink == "none" ? null : editorLink;

                    break;

                case TimeoutOption when i + 1 < args.Length && int.TryParse( args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds )
                                                             && seconds > 0:
                    options.DefaultTimeout = TimeSpan.FromSeconds( seconds );
                    i++;

                    break;

                case "--help" or "-h":
                    options.Error =
                        $"Usage: metalama-mcp [{TimeoutOption} <seconds>] [{EditorLinkOption} <template>|none] [{NoHttpOption}]\n\nA local MCP server that communicates over stdio only. Results in the html format are served on a random loopback port, unless {NoHttpOption} is given. Register it with an MCP client, for example:\n  claude mcp add metalama -- dnx Metalama.Mcp";

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
