// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.SyntaxGeneration;

internal sealed partial class ContextualSyntaxGenerator
{
    private sealed class NormalizeSpaceRewriter : SafeSyntaxRewriter
    {
        private readonly string _endOfLine;

        public NormalizeSpaceRewriter( string endOfLine )
        {
            this._endOfLine = endOfLine;
        }

#pragma warning disable LAMA0830 // NormalizeWhitespace is expensive.
        public override SyntaxNode VisitTupleType( TupleTypeSyntax node ) => base.VisitTupleType( node )!.NormalizeWhitespace( eol: this._endOfLine );
#pragma warning restore LAMA0830
    }
}