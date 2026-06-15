// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// A locally-captured exception/performance report, read back for review before sending. The report is self-contained:
/// it carries both renderings of the same report and its category, so the review page needs no out-of-band parameters.
/// See #1674.
/// </summary>
/// <param name="ScrubbedContent">The exact, scrubbed payload that would be uploaded.</param>
/// <param name="LocalContent">
/// The full, unscrubbed rendering of the same report, kept on the machine and shown next to <paramref name="ScrubbedContent"/>
/// so the user can see exactly what the scrubber removes. <c>null</c> if a full rendering is not available.
/// </param>
/// <param name="Category">The report category, which drives the per-category auto-report checkbox.</param>
public sealed record LocalExceptionReport( string ScrubbedContent, string? LocalContent, TelemetryScenario Category );
