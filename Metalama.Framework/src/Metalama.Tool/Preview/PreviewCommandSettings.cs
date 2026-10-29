// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using SharpCrafters.Backstage.Commands;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Metalama.Tool.Preview;

internal sealed class PreviewCommandSettings : BaseCommandSettings
{
    [UsedImplicitly]
    [Description( "The C# files whose transformed code is shown. All must belong to the same project." )]
    [CommandArgument( 0, "<files>" )]
    public string[] Files { get; init; } = [];

    [UsedImplicitly]
    [Description( "The project file. By default, the nearest project file in the directory of the first file or above." )]
    [CommandOption( "--project" )]
    public string? Project { get; init; }

    [UsedImplicitly]
    [Description( "The target framework. By default, the first target framework of the project." )]
    [CommandOption( "-f|--framework" )]
    public string? Framework { get; init; }

    [UsedImplicitly]
    [Description( "The build configuration. By default, the default configuration of the project." )]
    [CommandOption( "-c|--configuration" )]
    public string? Configuration { get; init; }

    [UsedImplicitly]
    [Description( "A directory where the transformed files are written. By default, they are written to the standard output." )]
    [CommandOption( "-o|--output" )]
    public string? Output { get; init; }

    [UsedImplicitly]
    [Description( "Do not restore the project before reading its compiler command line." )]
    [CommandOption( "--no-restore" )]
    public bool NoRestore { get; init; }
}
