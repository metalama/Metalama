// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Commands.Telemetry;

/// <summary>
/// Specifies which kind of telemetry the <c>telemetry enable</c> and <c>telemetry disable</c> commands configure.
/// </summary>
internal enum TelemetryScenarioArgument
{
    /// <summary>
    /// Anonymous usage reporting.
    /// </summary>
    Usage,

    /// <summary>
    /// Exception reporting.
    /// </summary>
    Exception,

    /// <summary>
    /// Performance-problem reporting.
    /// </summary>
    Performance,

    /// <summary>
    /// All of the above.
    /// </summary>
    All
}
