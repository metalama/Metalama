// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Introspection;
using Metalama.Framework.Services;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Workspaces;

internal sealed class WorkspaceIntrospectionService( Future<Workspace> workspace ) : IProjectService
{
    private readonly WorkspaceReferenceGraph _referenceGraph = new( workspace );

    public IIntrospectionReferenceGraph GetReferenceGraph() => this._referenceGraph;

    public IEnumerable<INamedType> GetDerivedTypes( INamedType type, bool directOnly )
        => workspace.Value.Projects.SelectMany(
            p =>
            {
                if ( !type.TryForCompilation( p.Compilation, out var translatedType ) )
                {
                    return [];
                }

                return p.Compilation.GetDerivedTypes( translatedType, directOnly ? DerivedTypesOptions.DirectOnly : DerivedTypesOptions.All );
            } );
}