// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// The entry point to telemetry. Telemetry is never globally enabled: it always requires an <see cref="ITelemetryContext"/>
/// obtained from <see cref="OpenContext"/> for a directory. The context resolves the repository-scoped <c>metalama.json</c>
/// opt-out and combines it with the process-level gates. See #1701.
/// </summary>
[PublicAPI]
public interface ITelemetryService : IBackstageService
{
    /// <summary>
    /// Opens a telemetry context for the given directory (a project, solution or repository directory). The context
    /// resolves the repository-root <c>metalama.json</c> by walking up from <paramref name="directory"/> and combines
    /// its opt-out with the process-level gates.
    /// </summary>
    ITelemetryContext OpenContext( string directory );

    /// <summary>
    /// Gets a disabled context for callers that have no directory (and therefore must not send telemetry). It still
    /// writes local crash reports for support, but collects and sends no telemetry. See #1701.
    /// </summary>
    ITelemetryContext NullContext { get; }
}
