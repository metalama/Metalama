// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Rewrites top-level <c>return;</c> statements inside a constructor body to
/// <c>goto &lt;labelName&gt;;</c> so that the <c>AfterLastInstanceConstructor</c> epilogue
/// (the call to <c>OnConstructed</c>) still fires on early-return paths.
/// </summary>
/// <remarks>
/// Rewrites are confined to the constructor body: nested lambdas, anonymous methods, and
/// local functions are left untouched because their <c>return</c> statements belong to a
/// different code path and must not be redirected to the constructor epilogue.
/// </remarks>
internal sealed class ConstructorEpilogueRewriter : SafeSyntaxRewriter
{
    private readonly string _labelName;

    public ConstructorEpilogueRewriter( string labelName )
    {
        this._labelName = labelName;
    }

    /// <summary>
    /// Gets a value indicating whether the rewriter found and replaced at least one
    /// <c>return;</c> statement. Callers use this to decide whether to emit the epilogue
    /// label — emitting an unused label would produce a CS0164 warning.
    /// </summary>
    public bool HasRewrites { get; private set; }

    public override SyntaxNode? VisitReturnStatement( ReturnStatementSyntax node )
    {
        // Constructors cannot `return value;`, so the expression is always null in well-formed
        // input — but guard defensively in case the input is malformed.
        if ( node.Expression != null )
        {
            return base.VisitReturnStatement( node );
        }

        this.HasRewrites = true;

        // Trivia is intentionally dropped: the goto is a system-generated replacement and the
        // linker's formatting pipeline will reformat the block after injection. The explicit
        // `goto` keyword token with trailing space is required so the output reads
        // `goto __metalama_epilogue;` and not `goto__metalama_epilogue;`.
        return GotoStatement(
            SyntaxKind.GotoStatement,
            SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.GotoKeyword ),
            default,
            SyntaxFactoryEx.SafeIdentifierName( this._labelName ),
            Token( SyntaxKind.SemicolonToken ) );
    }

    // Nested code regions with their own `return` semantics must not be rewritten.
    public override SyntaxNode? VisitSimpleLambdaExpression( SimpleLambdaExpressionSyntax node ) => node;

    public override SyntaxNode? VisitParenthesizedLambdaExpression( ParenthesizedLambdaExpressionSyntax node ) => node;

    public override SyntaxNode? VisitAnonymousMethodExpression( AnonymousMethodExpressionSyntax node ) => node;

    public override SyntaxNode? VisitLocalFunctionStatement( LocalFunctionStatementSyntax node ) => node;
}
