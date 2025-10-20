// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Extensibility;

public static class ContributorExtensions
{
    public static IEnumerable<T> OfKind<T>( this IEnumerable<IContributor> contributors, ContributorKind<T> kind )
        where T : IContributor
        => contributors.Where( c => c.ContributorKind == kind ).OfType<T>();

    public static IEnumerable<T> OfKind<T>( this ImmutableArray<IContributor> contributors, ContributorKind<T> kind )
        where T : IContributor
        => contributors.Where( c => c.ContributorKind == kind ).OfType<T>();

    public static IEnumerable<IExtensionPipelineContributor> Extensions( this IEnumerable<IContributor> contributors )
        => contributors.Where( c => c.ContributorKind.IsExtension ).Cast<IExtensionPipelineContributor>();

    public static IEnumerable<IExtensionPipelineContributor> Extensions( this ImmutableArray<IContributor> contributors )
        => contributors.Where( c => c.ContributorKind.IsExtension ).Cast<IExtensionPipelineContributor>();
}