// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.SyntaxGeneration;

public sealed partial class ContextualSyntaxGenerator
{
    private sealed class NormalizeSpaceRewriter( string endOfLine ) : SafeSyntaxRewriter
    {
#pragma warning disable LAMA0830 // NormalizeWhitespace is expensive.
        public override SyntaxNode VisitTupleType( TupleTypeSyntax node ) => base.VisitTupleType( node )!.NormalizeWhitespace( eol: endOfLine );
#pragma warning restore LAMA0830
    }
}