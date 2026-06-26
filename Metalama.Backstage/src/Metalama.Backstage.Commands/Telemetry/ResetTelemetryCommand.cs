// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Telemetry;

namespace Metalama.Backstage.Commands.Telemetry;

// Resets a scenario to TelemetryConsent.Default: review-first (ask) for exception/performance, and the opt-out default
// for usage. This is the third state that 'enable' (Yes) and 'disable' (No) cannot express. See #1674, #1707.
internal class ResetTelemetryCommand : SetTelemetryCommand
{
    public ResetTelemetryCommand() : base( TelemetryConsent.Default ) { }
}