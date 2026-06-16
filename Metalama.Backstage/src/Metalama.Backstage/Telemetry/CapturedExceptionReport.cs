// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// A locally-captured exception/performance report, read back for review before sending. A captured report has two
/// renderings of the same underlying error: the <em>scrubbed</em> payload that would be uploaded, and the full
/// <em>local</em> rendering that never leaves the machine. The record is self-contained — it also carries the report
/// category — so the review page needs no out-of-band parameters. This type is unrelated to <see cref="LocalExceptionReporter"/>,
/// which writes a separate human-readable crash dump. See #1674.
/// </summary>
/// <param name="ScrubbedContent">The exact, scrubbed payload that would be uploaded.</param>
/// <param name="LocalContent">
/// The full, unscrubbed local rendering of the same report, kept on the machine and shown next to <paramref name="ScrubbedContent"/>
/// so the user can see exactly what the scrubber removes. It is captured for every report (including auto-sent ones);
/// it is <c>null</c> only when no full rendering is available — a report captured through a custom exception adapter that
/// scrubs internally, or one whose local rendering has already been removed by the retention cleanup.
/// </param>
/// <param name="Category">The report category, which drives the per-category auto-report checkbox.</param>
/// <param name="IsQueued">
/// <c>true</c> if the report has already been enqueued for upload (auto-sent, or sent on a previous review). The review
/// page then shows it as already sent instead of offering the Report button again.
/// </param>
public sealed record CapturedExceptionReport( string ScrubbedContent, string? LocalContent, TelemetryScenario Category, bool IsQueued = false );
