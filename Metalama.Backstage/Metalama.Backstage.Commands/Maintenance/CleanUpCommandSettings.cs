// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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