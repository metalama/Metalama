// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Types;

namespace Metalama.Framework.Engine.CodeModel.Facets;

/// <summary>
/// Implementation of <see cref="IDelegateFacet"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class stores the type only. <see cref="InvokeMethod"/> is resolved by the derived class, from the symbol of
/// a type read from source and from the builder data of an introduced one, so constructing the facet of a type
/// whose signature is never read costs one allocation and no resolution.
/// </para>
/// </remarks>
internal abstract class DelegateFacet : IDelegateFacet
{
    protected DelegateFacet( INamedType type )
    {
        this.Type = type;
    }

    public TypeFacetKind FacetKind => TypeFacetKind.Delegate;

    public INamedType Type { get; }

    public abstract IMethod InvokeMethod { get; }

    public IType ReturnType => this.InvokeMethod.ReturnType;

    public IParameterList Parameters => this.InvokeMethod.Parameters;
}
