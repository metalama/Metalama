// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// A compile-time entry point that executes within the compiler and IDE to add aspects and implement architecture rules
    /// for a specific namespace and its nested namespaces.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Namespace fabrics are unique classes that execute at compile time and design time. Unlike aspects, fabrics do not need to be applied
    /// to any declaration or called from anywhere—their primary method (<see cref="AmendNamespace"/>) is invoked automatically simply because
    /// the class exists in the namespace.
    /// </para>
    /// <para>
    /// A <see cref="NamespaceFabric"/> applies transformations to the namespace that contains it, allowing you to scope aspect application
    /// and validation rules to specific namespace hierarchies. This provides more granular control than <see cref="ProjectFabric"/> but
    /// broader scope than <see cref="TypeFabric"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="Fabric"/>
    /// <seealso cref="INamespaceAmender"/>
    /// <seealso cref="ProjectFabric"/>
    /// <seealso cref="TypeFabric"/>
    /// <seealso href="@fabrics"/>
    /// <seealso href="@aspect-configuration"/>
    /// <seealso href="@fabrics-adding-aspects"/>
    /// <seealso href="@validation"/>
    public abstract class NamespaceFabric : Fabric
    {
        /// <summary>
        /// Implement this method to programmatically analyze the current namespace, add aspects, configure options, validate architecture, and report or suppress diagnostics.
        /// This method is invoked automatically at compile time and design time.
        /// </summary>
        /// <param name="amender">An object that provides access to query declarations in the namespace and its nested namespaces, add aspects using LINQ-like queries, configure options, and report diagnostics.</param>
        /// <seealso href="@fabrics-adding-aspects"/>
        /// <seealso href="@aspect-configuration"/>
        /// <seealso href="@validation"/>
        public abstract void AmendNamespace( INamespaceAmender amender );
    }
}