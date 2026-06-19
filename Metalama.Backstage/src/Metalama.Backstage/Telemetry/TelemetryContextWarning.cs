// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// A warning produced while resolving the repository configuration of a telemetry context (for example, a misplaced or
/// malformed <c>metalama.json</c>). It is surfaced on <see cref="ITelemetryContext.Warnings"/> for the caller to report
/// as a diagnostic. <see cref="FilePath"/> is the <c>metalama.json</c> the warning is about, so the caller can point the
/// diagnostic location at that file. See #1701.
/// </summary>
[PublicAPI]
public sealed record TelemetryContextWarning( string Message, string? FilePath );
