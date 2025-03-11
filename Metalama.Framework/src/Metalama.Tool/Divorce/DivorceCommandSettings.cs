// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Commands;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Metalama.Tool.Divorce;

internal sealed class DivorceCommandSettings : BaseCommandSettings
{
    [UsedImplicitly]
    [Description( "Force the divorce feature, even if the working directory is not known to be clean." )]
    [CommandOption( "--force" )]
    public bool Force { get; init; }
}