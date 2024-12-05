// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Validation;
using Metalama.Framework.Introspection;
using Metalama.Framework.Validation;
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