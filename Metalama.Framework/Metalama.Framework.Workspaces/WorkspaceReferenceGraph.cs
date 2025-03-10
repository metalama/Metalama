// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Introspection;
using Metalama.Framework.Project;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Workspaces;

internal sealed class WorkspaceReferenceGraph : IIntrospectionReferenceGraph
{
    private readonly Future<Workspace> _workspace;

    public WorkspaceReferenceGraph( Future<Workspace> workspace )
    {
        this._workspace = workspace;
    }

    public IEnumerable<IIntrospectionReference> GetInboundReferences(
        IDeclaration destination,
        IntrospectionChildKinds childKinds,
        CancellationToken cancellationToken )
    {
        return this._workspace.Value.Projects
            .SelectMany(
                project =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var service = project.Compilation.Project.ServiceProvider.GetRequiredService<IProjectIntrospectionService>();
                    var graph = service.GetReferenceGraph( project.Compilation );

                    return graph.GetInboundReferences( destination, childKinds, cancellationToken );
                } );
    }

    public IEnumerable<IIntrospectionReference> GetOutboundReferences( IDeclaration origin, CancellationToken cancellationToken = default )
    {
        var service = origin.Compilation.Project.ServiceProvider.GetRequiredService<IProjectIntrospectionService>();
        var graph = service.GetReferenceGraph( origin.Compilation );

        return graph.GetOutboundReferences( origin, cancellationToken );
    }
}