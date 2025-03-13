// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Project;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Fabrics;

/// <summary>
/// The non-generic base interface for <see cref="IQuery{TDeclaration}"/>. Represents query of declarations to which
/// aspects, validators, diagnostics and code fix suggestions can be added. This interface exposes LINQ-like methods that can be combined in complex queries.
/// </summary>
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