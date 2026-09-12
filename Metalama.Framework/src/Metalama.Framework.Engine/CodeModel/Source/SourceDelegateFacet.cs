// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Facets;
using Metalama.Framework.Engine.Utilities;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Source;

/// <summary>
/// The facet of a delegate read from source or from a referenced assembly.
/// </summary>
internal sealed class SourceDelegateFacet : DelegateFacet
{
    /// <summary>
    /// The identifier of the method that carries the signature of a delegate. This class is the single site of the
    /// code model that resolves it: every other consumer reaches the method through the facet.
    /// </summary>
    private const string _invokeMethodName = nameof(System.Action.Invoke);

    public SourceDelegateFacet( INamedType type ) : base( type ) { }

    [Memo]
    public override IMethod InvokeMethod => this.Type.Methods.OfName( _invokeMethodName ).Single();
}
