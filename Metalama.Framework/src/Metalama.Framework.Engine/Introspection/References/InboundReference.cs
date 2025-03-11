// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Introspection.References;

internal sealed class InboundReference( ISymbol referencedSymbol, ReferencingSymbolInfo referencingSymbolInfo, CompilationModel compilation )
    : IIntrospectionReference
{
    [Memo]
    public IDeclaration DestinationDeclaration => compilation.Factory.GetDeclaration( referencedSymbol );

    [Memo]
    public IDeclaration OriginDeclaration => compilation.Factory.GetDeclaration( referencingSymbolInfo.ReferencingSymbol );

    public ReferenceKinds Kinds => referencingSymbolInfo.Nodes.ReferenceKinds;

    [Memo]
    public IReadOnlyList<IntrospectionReferenceDetail> Details
        => referencingSymbolInfo.Nodes.SelectAsImmutableArray(
            n => new IntrospectionReferenceDetail(
                this,
                n.ReferenceKind,
                new SourceReference( n.Syntax.AsNode() ?? (object) n.Syntax.AsToken(), SourceReferenceImpl.Instance ) ) );

    public override string ToString() => $"{this.OriginDeclaration} -> {this.DestinationDeclaration}";
}