// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Repositories;

/// <summary>
/// The <c>telemetry</c> section of a <see cref="RepositoryConfiguration"/>.
/// </summary>
internal sealed record RepositoryTelemetryConfiguration
{
    /// <summary>
    /// Gets a value indicating whether telemetry is enabled for the repository. <c>false</c> opts the repository out;
    /// <c>true</c> opts it in (still subject to the process-level gates); <c>null</c> (absent) honors the global default.
    /// </summary>
    public bool? Enabled { get; init; }
}
