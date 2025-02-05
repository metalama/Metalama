// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Spectre.Console.Cli;
using System.ComponentModel;

namespace Metalama.Backstage.Commands.Licensing;

#pragma warning disable CS8618

internal class RegisterLicenseCommandSettings : BaseCommandSettings
{
    [Description( "The license key to be registered or unregistered, or 'trial' or 'free'." )]
    [CommandArgument( 1, "<license>" )]
    public string License { get; init; }
}