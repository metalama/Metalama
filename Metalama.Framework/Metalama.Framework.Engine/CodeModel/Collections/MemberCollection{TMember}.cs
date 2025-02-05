// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.UpdatableCollections;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal abstract class MemberCollection<TMember> : MemberOrNamedTypeCollection<TMember>
    where TMember : class, IMember
{
    public INamedType DeclaringType { get; }

    protected MemberCollection( INamedType declaringType, IUpdatableCollection<IFullRef<TMember>> sourceItems )
        : base( declaringType, sourceItems )
    {
        this.DeclaringType = declaringType;
    }
}