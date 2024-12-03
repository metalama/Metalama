// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal abstract class MemberUpdatableCollection<T> : DeclarationUpdatableCollection<T>
    where T : class, INamedDeclaration
{
    protected MemberUpdatableCollection( CompilationModel compilation, IRef containingDeclaration ) : base( compilation )
    {
        this._containingDeclaration = containingDeclaration;
    }

    private readonly IRef _containingDeclaration;

    protected abstract DeclarationKind ItemsDeclarationKind { get; }

    protected IEnumerable<IFullRef<T>> GetMemberRefsOfName( string name )
        => this._containingDeclaration.AsFullRef()
            .GetMembersOfName(
                name,
                this.ItemsDeclarationKind,
                this.Compilation )
            .Cast<IFullRef<T>>();

    protected IEnumerable<IFullRef<T>> GetMemberRefs()
        => this._containingDeclaration.AsFullRef()
            .GetMembers( this.ItemsDeclarationKind, this.Compilation )
            .Cast<IFullRef<T>>();
}