// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Metalama.Backstage.Commands;

[UsedImplicitly( ImplicitUseTargetFlags.WithInheritors | ImplicitUseTargetFlags.WithMembers )]
public class BaseCommandSettings : CommandSettings
{
    [Description( "Prints trace messages." )]
    [CommandOption( "--verbose" )]
    public bool Verbose { get; protected set; }

    [Description( "Does not report any warnings." )]
    [CommandOption( "--no-warn" )]
    public bool NoWarn { get; protected set; }

    [Description( "Attaches a debugger." )]
    [CommandOption( "--debug", IsHidden = true )]
    public bool Debug { get; protected set; }
    
    [CommandOption( "--dev", IsHidden = true )]
    public bool IsDevelopmentEnvironment { get; init; }
    
    // For testing only, adds backstage UI services.
    [CommandOption( "--with-ui", IsHidden = true )]
    public bool AddUserInterface { get; init; }
}