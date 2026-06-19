// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// The reporting action configured for a telemetry scenario (see <see cref="TelemetryScenario"/>). Its meaning depends
/// on the scenario:
/// <list type="bullet">
/// <item>For <see cref="TelemetryScenario.Usage"/> (opt-out): both <see cref="Default"/> and <see cref="Yes"/> mean the
/// usage session is collected and sent; only <see cref="No"/> disables it.</item>
/// <item>For <see cref="TelemetryScenario.Exception"/> and <see cref="TelemetryScenario.Performance"/>: <see cref="Default"/>
/// means ASK (the report is captured locally and a review toast is shown, but it is not auto-sent); <see cref="Yes"/>
/// additionally auto-sends; <see cref="No"/> means the report is not even captured and the user is not asked.</item>
/// </list>
/// See #1674, #1701.
/// </summary>
[PublicAPI]
public enum ReportingAction
{
    /// <summary>
    /// For usage, enabled (opt-out). For exception/performance, ASK: capture locally and show a review toast, but do not
    /// auto-send.
    /// </summary>
    Default,

    /// <summary>
    /// Enabled and auto-sent. For exception/performance, the report is captured, the user is notified, and the report is
    /// enqueued for upload without waiting for review.
    /// </summary>
    Yes,

    /// <summary>
    /// Disabled. For usage, the session is not collected. For exception/performance, the report is not captured and the
    /// user is not asked.
    /// </summary>
    No
}