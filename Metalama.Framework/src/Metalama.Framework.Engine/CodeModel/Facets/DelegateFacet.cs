// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.Utilities;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Facets;

/// <summary>
/// Implementation of <see cref="IDelegateFacet"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class stores the type only. Every other member is resolved on first read and memoized, so constructing the
/// facet of a type whose signature is never read costs one allocation and no resolution.
/// </para>
/// </remarks>
internal sealed class DelegateFacet : IDelegateFacet
{
    /// <summary>
    /// The identifier of the method that carries the signature of a delegate. This class is the single site of the
    /// code model that resolves it: every other consumer reaches the method through the facet.
    /// </summary>
    private const string _invokeMethodName = nameof(System.Action.Invoke);

    public DelegateFacet( INamedType type )
    {
        this.Type = type;
    }

    public TypeFacetKind FacetKind => TypeFacetKind.Delegate;

    public INamedType Type { get; }

    [Memo]
    public IMethod InvokeMethod => this.GetInvokeMethodCore();

    private IMethod GetInvokeMethodCore()
        => this.Type switch
        {
            // The Invoke method of an introduced delegate comes from the builder data, which answers in every
            // compilation that knows the delegate rather than only in one to which the transformation that registers
            // the method has been applied. An aspect that types an event by a delegate it has just introduced reads
            // the facet through a builder whose compilation is the one the aspect sees, which is not the compilation
            // the advice writes to.
            IntroducedNamedType { InvokeMethod: { } invokeMethod } => invokeMethod,
            _ => this.Type.Methods.OfName( _invokeMethodName ).Single()
        };

    public IType ReturnType => this.InvokeMethod.ReturnType;

    public IParameterList Parameters => this.InvokeMethod.Parameters;
}
