// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace Metalama.Backstage.Worker.WebServer;

[UsedImplicitly]
internal class WebServerCommandSettings : CommandSettings
{
    [CommandOption( "--port" )]
    public int Port
    {
        get;
        [UsedImplicitly]
        init;
    } = 5252;

    /// <summary>
    /// Gets the path of the file containing the per-session authentication token that the server must require.
    /// When not specified, the server generates a token of its own and prints the URL carrying it.
    /// </summary>
    /// <remarks>
    /// The token is passed by path rather than by value because a command line is readable by any local user, which
    /// is precisely the audience the token defends against. See <c>SetupWebServerToken</c>.
    /// </remarks>
    [CommandOption( "--token-file" )]
    public string? TokenFile
    {
        get;
        [UsedImplicitly]
        init;
    }
}