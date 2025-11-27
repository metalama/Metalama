// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Services;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Provides introspection services for a project.
/// </summary>
/// <seealso cref="IProjectService"/>
/// <seealso href="@introspection-api"/>
internal interface IProjectIntrospectionService : IProjectService
{
    /// <summary>
    /// Gets the reference graph for a compilation.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <returns>The reference graph.</returns>
    IIntrospectionReferenceGraph GetReferenceGraph( ICompilation compilation );
}