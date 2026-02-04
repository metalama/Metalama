// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Shared functionality for async method inliners that handle await expressions.
/// </summary>
internal abstract class AsyncMethodInliner : Inliner
{
    public override bool IsValidForTargetSymbol( ISymbol symbol )
        => symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol { MethodKind: not MethodKind.Constructor, AssociatedSymbol: null, IsAsync: true } methodSymbol
           && !methodSymbol.IsIteratorMethod();

    public override bool IsValidForContainingSymbol( ISymbol symbol ) => true;

    /// <summary>
    /// Unwraps parentheses from an expression and returns the inner await expression if present.
    /// Handles patterns like: (await x), ((await x)), await x
    /// </summary>
    protected static AwaitExpressionSyntax? GetAwaitExpression( ExpressionSyntax expression )
        => expression.RemoveParenthesis() as AwaitExpressionSyntax;

    /// <summary>
    /// Gets the invocation expression from an await expression.
    /// </summary>
    protected static InvocationExpressionSyntax? GetInvocationFromAwait( AwaitExpressionSyntax awaitExpression )
        => awaitExpression.Expression as InvocationExpressionSyntax;
}
