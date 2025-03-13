// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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