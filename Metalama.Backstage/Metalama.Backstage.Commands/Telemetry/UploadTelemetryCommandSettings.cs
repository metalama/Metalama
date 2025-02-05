// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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