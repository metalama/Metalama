// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;

namespace Metalama.Framework.Code;

/// <summary>
/// Represents a namespace or a named type.
/// </summary>
public interface INamespaceOrNamedType : INamedDeclaration
{
    /// <summary>
    /// Gets the full name of the namespace or type, or an empty string if this is the global namespace.
    /// The separator for nested types is the dot, and there is no suffix for generic types.
    /// </summary>
    string FullName { get; }

    /// <summary>
    /// Gets the list of types defined in the current namespace or type. 
    /// </summary>
    /// <remarks>
    /// If the current object is an <see cref="INamedType"/>,
    /// this is the collection of nested types. If the current object is an <see cref="INamespace"/>, the collection only
    /// includes types inside the <see cref="IDeclaration.DeclaringAssembly"/>. In case of partial compilations (see <see cref="INamespace.IsPartial"/>),
    /// this collection only contain the types in the current partial compilation.
    /// </remarks>
    INamedTypeCollection Types { get; }

    /// <summary>
    /// Gets the parent namespace, or <c>null</c> if the current object is the global namespace
    /// (i.e. ig <see cref="INamespace.IsGlobalNamespace"/> is <c>true</c>).
    /// </summary>
    INamespace? ContainingNamespace { get; }

    new IRef<INamespaceOrNamedType> ToRef();
}