// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Provides methods for querying references between declarations in a compilation. This interface enables
/// static code analysis by exposing both inbound references (declarations that reference a target) and
/// outbound references (declarations that a source declaration references).
/// </summary>
/// <seealso cref="IIntrospectionReference"/>
/// <seealso cref="IntrospectionChildKinds"/>
[PublicAPI]
public interface IIntrospectionReferenceGraph
{
    /// <summary>
    /// Gets all inbound references to a specified declaration, i.e., all code locations that reference the destination.
    /// </summary>
    /// <param name="destination">The declaration to find references to.</param>
    /// <param name="childKinds">Specifies whether to include references to child declarations.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of all references pointing to the destination declaration.</returns>
    IEnumerable<IIntrospectionReference> GetInboundReferences(
        IDeclaration destination,
        IntrospectionChildKinds childKinds = IntrospectionChildKinds.ContainingDeclaration,
        CancellationToken cancellationToken = default );

    /// <summary>
    /// Gets all outbound references from a specified declaration, i.e., all declarations that the origin references.
    /// </summary>
    /// <param name="origin">The declaration to find references from.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of all references from the origin declaration to other declarations.</returns>
    IEnumerable<IIntrospectionReference> GetOutboundReferences( IDeclaration origin, CancellationToken cancellationToken = default );
}