// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using System.Collections;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal abstract class AllMembersCollection<T> : AllMemberOrNamedTypesCollection<T, IMemberCollection<T>>, IMemberCollection<T>
    where T : class, IMember
{
    protected AllMembersCollection( INamedTypeImpl declaringType ) : base( declaringType ) { }

    INamedType IMemberCollection<T>.DeclaringType => this.DeclaringType;

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}