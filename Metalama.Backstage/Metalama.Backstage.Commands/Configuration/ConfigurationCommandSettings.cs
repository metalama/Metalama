// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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