// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Spectre.Console.Cli;
using System.ComponentModel;

namespace Metalama.Backstage.Commands.Telemetry;

internal class UploadTelemetryCommandSettings : BaseCommandSettings
{
    [Description( "Run the upload asynchronously in a background process." )]
    [CommandOption( "--async | -a" )]
    public bool Async { get; init; }

    [Description( "Force the upload even if another upload has been performed recently." )]
    [CommandOption( "--force | -f" )]
    public bool Force { get; init; }
}