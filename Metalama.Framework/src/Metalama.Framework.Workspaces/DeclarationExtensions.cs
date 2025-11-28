// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Introspection;
using Metalama.Framework.Project;
using System.Collections.Generic;
using System.Threading;

namespace Metalama.Framework.Workspaces;

/// <summary>
/// Extension methods for <see cref="IDeclaration"/> that provide introspection capabilities within a workspace.
/// </summary>
/// <seealso cref="IDeclaration"/>
/// <seealso cref="Workspace"/>
/// <seealso cref="IIntrospectionReference"/>
/// <seealso href="@introspection-api"/>
[PublicAPI]
public static class DeclarationExtensions
{
    /// <summary>
    /// Gets inbound declaration references, i.e. the list of declarations that use the given declaration,
    /// in the projects loaded in the current <see cref="Workspace"/>.
    /// </summary>
    /// <param name="declaration">The declaration to get inbound references for.</param>
    /// <param name="childKinds">Specifies which kinds of child declarations to include.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An enumerable of inbound references.</returns>
    public static IEnumerable<IIntrospectionReference> GetInboundReferences(
        this IDeclaration declaration,
        IntrospectionChildKinds childKinds = IntrospectionChildKinds.ContainingDeclaration,
        CancellationToken cancellationToken = default )
    {
        var service = declaration.Compilation.Project.ServiceProvider.GetRequiredService<WorkspaceIntrospectionService>();
        var graph = service.GetReferenceGraph();

        return graph.GetInboundReferences( declaration, childKinds, cancellationToken );
    }

    /// <summary>
    /// Gets outbound declaration references, i.e. the list of declarations that the given declaration uses,
    /// in the projects loaded in the current <see cref="Workspace"/>.
    /// </summary>
    /// <param name="declaration">The declaration to get outbound references for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An enumerable of outbound references.</returns>
    public static IEnumerable<IIntrospectionReference> GetOutboundReferences(
        this IDeclaration declaration,
        CancellationToken cancellationToken = default )
    {
        var service = declaration.Compilation.Project.ServiceProvider.GetRequiredService<IProjectIntrospectionService>();
        var graph = service.GetReferenceGraph( declaration.Compilation );

        return graph.GetOutboundReferences( declaration, cancellationToken );
    }

    /// <summary>
    /// Get all types derived from a given type within the projects loaded in the current <see cref="Workspace"/>.
    /// </summary>
    /// <param name="type">The base type.</param>
    /// <param name="directOnly">If <c>true</c>, only types directly derived from <paramref name="type"/> are returned; otherwise, all derived types in the inheritance hierarchy are returned.</param>
    /// <returns>An enumerable of derived types.</returns>
    public static IEnumerable<INamedType> GetDerivedTypes( this INamedType type, bool directOnly = false )
    {
        var service = type.Compilation.Project.ServiceProvider.GetRequiredService<WorkspaceIntrospectionService>();

        return service.GetDerivedTypes( type, directOnly );
    }
}