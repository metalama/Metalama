// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Spectre.Console.Cli;
using System.ComponentModel;

namespace Metalama.Backstage.Commands.Configuration;

#pragma warning disable CS8618

internal class ConfigurationCommandSettings : BaseCommandSettings
{
    [Description( "The alias of the configuration file. Use the 'configuration list' command to list available configuration files." )]
    [CommandArgument( 0, "<alias>" )]
    public string Alias { get; init; }
}