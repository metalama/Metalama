// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Services;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Provides introspection services for a project, including access to the reference graph for analyzing
/// code relationships between declarations.
/// </summary>
/// <seealso cref="IProjectService"/>
/// <seealso cref="IIntrospectionReferenceGraph"/>
/// <seealso href="@introspection-api"/>
[PublicAPI]
[InternalImplement]
public interface IProjectIntrospectionService : IProjectService
{
    /// <summary>
    /// Gets the reference graph for a compilation, which enables querying inbound and outbound
    /// references between declarations.
    /// </summary>
    /// <param name="compilation">The compilation to get the reference graph for.</param>
    /// <returns>An <see cref="IIntrospectionReferenceGraph"/> for the specified compilation.</returns>
    IIntrospectionReferenceGraph GetReferenceGraph( ICompilation compilation );
}