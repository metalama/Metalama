// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// The parameter passed to <see cref="ProjectFabric.AmendProject"/> and <see cref="TransitiveProjectFabric.AmendProject"/>.
    /// Provides capabilities to query declarations across the project, add aspects programmatically using LINQ-like queries,
    /// configure options, report diagnostics, and validate architecture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Through this interface, you can access all types, namespaces, and other declarations in the project using LINQ-like queries.
    /// For example, you can select all types in a namespace, filter methods by name or attributes, and apply aspects to the results.
    /// </para>
    /// </remarks>
    /// <seealso cref="ProjectFabric"/>
    /// <seealso cref="TransitiveProjectFabric"/>
    /// <seealso cref="IAmender{T}"/>
    /// <seealso cref="ICompilation"/>
    /// <seealso href="@fabrics"/>
    /// <seealso href="@fabrics-adding-aspects"/>
    /// <seealso href="@aspect-configuration"/>
    /// <seealso href="@validation"/>
    public interface IProjectAmender : IAmender<ICompilation>;
}