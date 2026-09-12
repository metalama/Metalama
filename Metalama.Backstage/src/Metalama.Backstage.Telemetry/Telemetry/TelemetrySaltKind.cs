// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Telemetry;

public enum TelemetrySaltKind
{
    /// <summary>
    /// All data uploaded to Matomo.
    /// </summary>
    Matomo,

    /// <summary>
    /// Product usage tracking (bits.postsharp.net).
    /// </summary>
    UsageTracking,

    /// <summary>
    /// Exception and performance reports (bits.postsharp.net).
    /// </summary>
    ExceptionReport,

    /// <summary>
    /// License audit (bits.postsharp.net).
    /// </summary>
    LicenseAudit
}