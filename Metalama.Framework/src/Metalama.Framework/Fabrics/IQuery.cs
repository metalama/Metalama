// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Project;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Fabrics;

/// <summary>
/// The non-generic base interface for <see cref="IQuery{TDeclaration}"/>. Represents a query over code declarations
/// that can be used to programmatically select declarations and apply aspects, configuration, validators, and diagnostics.
/// Provides LINQ-like methods that can be combined to create complex queries.
/// </summary>
/// <remarks>
/// <para>
/// Query interfaces are used within fabrics to select declarations programmatically using method chaining and LINQ-like operations.
/// For example, you can select all types, filter by namespace or attributes, and then apply aspects to the results.
/// </para>
/// </remarks>
/// <seealso cref="IQuery{TDeclaration}"/>
/// <seealso cref="IAmender"/>
/// <seealso cref="QueryExtensions"/>
/// <seealso href="@fabrics"/>
/// <seealso href="@fabrics-adding-aspects"/>
[InternalImplement]
[CompileTime]
public interface IQuery
{
    /// <summary>
    /// Gets the current project.
    /// </summary>
    IProject Project { get; }

    /// <summary>
    /// Gets the current namespace, i.e. the one of the originating fabric or aspect instance,
    /// or <c>null</c> if the current object does not belong to a namespace.
    /// </summary>
    string? OriginatingNamespace { get; }

    /// <summary>
    /// Gets the declaration of the originating fabric or aspect instance.
    /// </summary>
    IRef<IDeclaration> OriginatingDeclaration { get; }
}