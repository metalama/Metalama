// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Project;
using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Fabrics
{
    [InternalImplement]
    [CompileTime]
    public interface IAmender
    {
        /// <summary>
        /// Gets the project being built.
        /// </summary>
        IProject Project { get; }
    }

    /// <summary>
    /// Base interface for the parameter passed to fabric methods such as <see cref="ProjectFabric.AmendProject"/>, <see cref="NamespaceFabric.AmendNamespace"/>,
    /// or <see cref="TypeFabric.AmendType"/>. Provides capabilities to query declarations, add aspects programmatically, configure options, report diagnostics, and validate architecture.
    /// </summary>
    /// <typeparam name="T">The type of declaration that this amender operates on (e.g., <see cref="ICompilation"/> for projects, <see cref="INamespace"/> for namespaces, <see cref="INamedType"/> for types).</typeparam>
    /// <remarks>
    /// <para>
    /// The amender interface provides access to LINQ-like querying capabilities through <see cref="IQuery{T}"/>, allowing you to select declarations
    /// and apply aspects or configuration to them. It combines querying, aspect application, option configuration, and diagnostic reporting in a single interface.
    /// </para>
    /// <para>
    /// Extension methods for this interface are provided by:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="Aspects.AspectQueryExtensions"/> - for adding aspects to selected declarations</description></item>
    /// <item><description><see cref="Options.OptionQueryExtensions"/> - for configuring options on selected declarations</description></item>
    /// <item><description><see cref="Diagnostics.DiagnosticsQueryExtensions"/> - for reporting diagnostics and suppressions</description></item>
    /// <item><description><see cref="QueryExtensions"/> - for additional querying capabilities</description></item>
    /// <item><description><c>Metalama.Extensions.Architecture.Predicates.PredicateExtensions</c> - for architecture validation predicates</description></item>
    /// <item><description><c>Metalama.Extensions.Validation.ValidationQueryExtensions</c> - for validation rules</description></item>
    /// <item><description><c>Metalama.Extensions.Validation.ReferenceValidationQueryExtensions</c> - for reference validation</description></item>
    /// <item><description><c>Metalama.Extensions.CodeFixes.CodeFixQueryExtensions</c> - for code fix suggestions</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ProjectFabric"/>
    /// <seealso cref="NamespaceFabric"/>
    /// <seealso cref="TypeFabric"/>
    /// <seealso cref="IProjectAmender"/>
    /// <seealso cref="INamespaceAmender"/>
    /// <seealso cref="ITypeAmender"/>
    /// <seealso cref="Aspects.AspectQueryExtensions"/>
    /// <seealso cref="Options.OptionQueryExtensions"/>
    /// <seealso cref="Diagnostics.DiagnosticsQueryExtensions"/>
    /// <seealso cref="QueryExtensions"/>
    /// <seealso href="@fabrics"/>
    /// <seealso href="@fabrics-adding-aspects"/>
    /// <seealso href="@aspect-configuration"/>
    /// <seealso href="@validation"/>
    public interface IAmender<out T> : IAmender, IQuery<T>
        where T : class, IDeclaration
    {
        new IProject Project { get; }

        /// <summary>
        /// Gets an object that allows to add child advice and to validate code and code references.
        /// </summary>
        [Obsolete( "The Outbound interface is now directly implemented by IAmender<T>." )]
        IQuery<T> Outbound { get; }
    }
}