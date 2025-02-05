// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.UpdatableCollections;
using System;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal sealed class AllImplementedInterfacesCollection : DeclarationCollection<INamedType>, IImplementedInterfaceCollection
{
    public AllImplementedInterfacesCollection( INamedTypeImpl declaringType, AllInterfaceUpdatableCollection source ) : base( declaringType, source ) { }

    public bool Contains( INamedType namedType ) => ((AllInterfaceUpdatableCollection) this.Source).Contains( namedType.ToRef() );

    public bool Contains( Type type )
    {
        var itype = this.ContainingDeclaration!.Compilation.Factory.GetTypeByReflectionType( type );

        if ( itype is not INamedType namedType )
        {
            return false;
        }

        return this.Contains( namedType );
    }
}