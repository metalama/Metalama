// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Introspection;
using Metalama.Framework.Project;
using System.Collections.Generic;
using System.Threading;

namespace Metalama.Framework.Workspaces;

[PublicAPI]
public static class DeclarationExtensions
{
    /// <summary>
    /// Gets inbound declaration references, i.e. the list of declarations that use the given declaration,
    /// in the projects loaded in the current <see cref="Workspace"/>. 
    /// </summary>
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
    /// Gets inbound declaration references, i.e. the list of declarations that use the given declaration,
    /// in the projects loaded in the current <see cref="Workspace"/>. 
    /// </summary>
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
    /// <param name="type"></param>
    /// <param name="directOnly">If <c>true</c>, only types directly derived from <paramref name="type"/> are returned.</param>
    public static IEnumerable<INamedType> GetDerivedTypes( this INamedType type, bool directOnly = false )
    {
        var service = type.Compilation.Project.ServiceProvider.GetRequiredService<WorkspaceIntrospectionService>();

        return service.GetDerivedTypes( type, directOnly );
    }
}