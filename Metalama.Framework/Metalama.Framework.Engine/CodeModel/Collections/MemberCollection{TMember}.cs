// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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