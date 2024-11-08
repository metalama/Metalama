// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal abstract class MemberUpdatableCollection<T>( CompilationModel compilation, IRef containingDeclaration )
    : DeclarationUpdatableCollection<T>( compilation )
    where T : class, INamedDeclaration
{
    protected abstract DeclarationKind ItemsDeclarationKind { get; }

    protected IEnumerable<IFullRef<T>> GetMemberRefsOfName( string name )
        => containingDeclaration.AsFullRef()
            .GetMembersOfName(
                name,
                this.ItemsDeclarationKind,
                this.Compilation )
            .Cast<IFullRef<T>>();

    protected IEnumerable<IFullRef<T>> GetMemberRefs()
        => containingDeclaration.AsFullRef()
            .GetMembers( this.ItemsDeclarationKind, this.Compilation )
            .Cast<IFullRef<T>>();
}