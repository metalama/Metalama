// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Repositories;

/// <summary>
/// Represents the content of a <c>metalama.json</c> file located at the root of a repository. This is a general,
/// extensible repository-scoped configuration file; telemetry is its first setting. Unlike
/// <see cref="Metalama.Backstage.Configuration.ConfigurationFile"/>, which lives in the per-user application-data
/// directory, this file is committed to source control and discovered by walking up the directory tree to the
/// repository root (see <see cref="IRepositoryConfigurationService"/>).
/// </summary>
internal sealed record RepositoryConfiguration
{
    /// <summary>
    /// Gets the telemetry-related settings, or <c>null</c> when the file does not configure telemetry.
    /// </summary>
    public RepositoryTelemetryConfiguration? Telemetry { get; init; }
}
