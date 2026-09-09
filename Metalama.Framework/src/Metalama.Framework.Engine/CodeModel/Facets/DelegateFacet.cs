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
/// The <c>Invoke</c> method is resolved by <see cref="TypeFacetCollection.Create"/> and passed to the constructor,
/// because the presence of that method is what decides whether the type has the facet at all. The other members are
/// properties of the method, and each of them is memoized by the method, so this class stores no other state and
/// computes nothing.
/// </para>
/// </remarks>
internal sealed class DelegateFacet : IDelegateFacet
{
    public DelegateFacet( INamedType type, IMethod invokeMethod )
    {
        this.Type = type;
        this.InvokeMethod = invokeMethod;
    }

    public TypeFacetKind FacetKind => TypeFacetKind.Delegate;

    public INamedType Type { get; }

    public IMethod InvokeMethod { get; }

    public IType ReturnType => this.InvokeMethod.ReturnType;

    public IParameterList Parameters => this.InvokeMethod.Parameters;
}
