// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;

namespace Metalama.Backstage.Repositories;

/// <summary>
/// The result of resolving the <c>metalama.json</c> for a given directory: the effective <see cref="Configuration"/>
/// (never <c>null</c>; an empty configuration when no file applies) and any <see cref="Warnings"/> that the caller should
/// surface to the user (for example, as a compiler warning or an IDE notification).
/// </summary>
internal sealed record RepositoryConfigurationResult
{
    public static RepositoryConfigurationResult Empty { get; } = new();

    public RepositoryConfiguration Configuration { get; init; } = new();

    public ImmutableArray<RepositoryConfigurationWarning> Warnings { get; init; } = ImmutableArray<RepositoryConfigurationWarning>.Empty;
}
