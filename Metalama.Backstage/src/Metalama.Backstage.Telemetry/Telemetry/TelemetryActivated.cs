// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// The event published by the telemetry services when the current process has enabled usage telemetry for the first
/// time on the machine. The user interface subscribes to it to inform the user and to open the welcome page, and the
/// news client subscribes to it to enable itself.
/// </summary>
[PublicAPI]
public sealed record TelemetryActivated;
