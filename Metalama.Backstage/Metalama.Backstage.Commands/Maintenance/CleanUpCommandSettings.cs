// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Spectre.Console.Cli;
using System.ComponentModel;

namespace Metalama.Backstage.Commands.Maintenance;

internal class CleanUpCommandSettings : BaseCommandSettings
{
    [Description( "Deletes all directories and files ignoring clean-up policies." )]
    [CommandOption( "--all" )]
    public bool All { get; init; }

    [Description( "Does not kill processes before clean-up." )]
    [CommandOption( "--no-kill" )]
    public bool DoNotKill { get; init; }
}