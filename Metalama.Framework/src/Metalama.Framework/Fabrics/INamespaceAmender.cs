// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// The parameter passed to <see cref="NamespaceFabric.AmendNamespace"/>.
    /// Provides capabilities to query declarations within the namespace and its nested namespaces, add aspects programmatically using LINQ-like queries,
    /// configure options, report diagnostics, and validate architecture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Through this interface, you can access all types and nested namespaces within the namespace using LINQ-like queries,
    /// allowing you to scope aspect application and configuration to specific namespace hierarchies.
    /// </para>
    /// </remarks>
    /// <seealso cref="NamespaceFabric"/>
    /// <seealso cref="IAmender{T}"/>
    /// <seealso cref="INamespace"/>
    /// <seealso href="@fabrics"/>
    /// <seealso href="@fabrics-adding-aspects"/>
    /// <seealso href="@aspect-configuration"/>
    /// <seealso href="@validation"/>
    public interface INamespaceAmender : IAmender<INamespace>
    {
        /// <summary>
        /// Gets the full name of the namespace on which the current fabric is applied.
        /// </summary>
        string Namespace { get; }
    }
}