// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// The entry point to telemetry. Telemetry is never globally enabled: it always requires an <see cref="ITelemetryContext"/>
/// obtained from <see cref="OpenContext"/> with an <see cref="ITelemetryPolicy"/>. The default policy
/// (<see cref="GetPolicy(string)"/>) resolves the repository-scoped <c>metalama.json</c> opt-out and combines it with the
/// process-level gates; a caller may supply its own policy, which fully replaces the default for that context. See #1701.
/// </summary>
[PublicAPI]
public interface ITelemetryService : IBackstageService
{
    /// <summary>
    /// Gets the default <see cref="ITelemetryPolicy"/> for the given directory (a project, solution or repository
    /// directory): it resolves the repository-root <c>metalama.json</c> by walking up from <paramref name="directory"/>
    /// and combines its opt-out with the process-level and per-category gates. When <paramref name="directory"/> is
    /// <c>null</c> or empty (the directory is unknown), telemetry is disabled. Pass the result to
    /// <see cref="OpenContext"/>, or wrap it to compose with a host-supplied consent while still honoring
    /// <c>metalama.json</c>.
    /// </summary>
    ITelemetryPolicy GetPolicy( string? directory );

    /// <summary>
    /// Gets the <see cref="ITelemetryPolicy"/> for callers that report telemetry about the tooling itself (the CLI, the
    /// worker, the compiler outer fallback) rather than about a specific project. It uses the process working directory
    /// as the context: when the working directory is inside a git repository, it honors that repository's policy
    /// (including its <c>metalama.json</c> opt-out); otherwise there is no repository to consult and telemetry is
    /// disabled. Local crash reports are written regardless. See #1701.
    /// </summary>
    ITelemetryPolicy GetToolingPolicy();

    /// <summary>
    /// Opens a telemetry context governed by the given <paramref name="policy"/>. The policy is the single authority for
    /// enablement; obtain the default from <see cref="GetPolicy(string)"/> or supply your own.
    /// </summary>
    ITelemetryContext OpenContext( ITelemetryPolicy policy );

    /// <summary>
    /// Gets a disabled context for callers that have no directory (and therefore must not send telemetry). It still
    /// writes local crash reports for support, but collects and sends no telemetry. See #1701.
    /// </summary>
    ITelemetryContext NullContext { get; }
}
