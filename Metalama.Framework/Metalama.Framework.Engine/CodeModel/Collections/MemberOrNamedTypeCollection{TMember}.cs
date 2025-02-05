// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.UpdatableCollections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal abstract class MemberOrNamedTypeCollection<TMember> : DeclarationCollection<TMember>, IMemberOrNamedTypeCollection<TMember>
    where TMember : class, IMemberOrNamedType
{
    protected MemberOrNamedTypeCollection( IDeclaration containingDeclaration, IReadOnlyList<IFullRef<TMember>> sourceItems )
        : base( containingDeclaration, sourceItems ) { }

    public IEnumerable<TMember> OfName( string name )
    {
        var typedSource = (IUpdatableCollection<IFullRef<TMember>>) this.Source;

        // Enumerate the source without causing a resolution of the reference.
        foreach ( var sourceItem in typedSource.OfName( name ) )
        {
            // Resolve the reference and store the declaration.
            var member = this.GetItem( sourceItem );

            // Return the result.
            yield return member;
        }
    }
}