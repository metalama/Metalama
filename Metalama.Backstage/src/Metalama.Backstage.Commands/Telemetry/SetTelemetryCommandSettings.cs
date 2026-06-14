// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Spectre.Console.Cli;
using System.ComponentModel;

namespace Metalama.Backstage.Commands.Telemetry;

internal class SetTelemetryCommandSettings : BaseCommandSettings
{
    [Description( "The kind of telemetry to configure: 'usage', 'exception', 'performance' or 'all'. Defaults to 'all'." )]
    [CommandArgument( 0, "[scenario]" )]
    [DefaultValue( TelemetryScenarioArgument.All )]
    public TelemetryScenarioArgument Scenario { get; init; } = TelemetryScenarioArgument.All;
}
