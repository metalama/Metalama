// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Introspection.References;

internal sealed class OutboundReference(
    ISymbol referencedSymbol,
    ISymbol referencingSymbol,
    IEnumerable<ReferenceGraph.OutboundReference> references,
    CompilationModel compilation )
    : IIntrospectionReference
{
    [Memo]
    public IDeclaration DestinationDeclaration => compilation.Factory.GetDeclaration( referencedSymbol );

    [Memo]
    public IDeclaration OriginDeclaration => compilation.Factory.GetDeclaration( referencingSymbol );

    [Memo]
    public ReferenceKinds Kinds => references.Select( r => r.ReferenceKind ).Union();

    [Memo]
    public IReadOnlyList<IntrospectionReferenceDetail> Details
        => references.Select(
                r => new IntrospectionReferenceDetail(
                    this,
                    r.ReferenceKind,
                    new SourceReference( r.Node.AsNode() ?? (object) r.Node.AsToken(), SourceReferenceImpl.Instance ) ) )
            .ToReadOnlyList();

    public override string ToString() => $"{this.OriginDeclaration} -> {this.DestinationDeclaration}";
}