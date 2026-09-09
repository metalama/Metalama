// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;

namespace Metalama.Framework.Engine.CodeModel.Facets;

/// <summary>
/// Implementation of <see cref="IUnionCase"/>.
/// </summary>
/// <remarks>
/// <para>
/// The three values are resolved by <see cref="UnionFacet"/> when it builds the case list, because none of them can
/// be computed from another one. The type of the case is the type of the single parameter of the creation member.
/// </para>
/// </remarks>
internal sealed class UnionCase : IUnionCase
{
    public UnionCase( IType type, int index, IMethodBase creationMember )
    {
        this.Type = type;
        this.Index = index;
        this.CreationMember = creationMember;
    }

    public IType Type { get; }

    public int Index { get; }

    public IMethodBase CreationMember { get; }

    public override string ToString() => $"{this.Index}: {this.Type}";
}
