// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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