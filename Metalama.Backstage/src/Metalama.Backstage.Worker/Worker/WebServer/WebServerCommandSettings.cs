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
    public int Port { get; set; } = 5252;
}