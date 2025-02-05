// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Shared functionality for method inliners.
/// </summary>
internal abstract class MethodInliner : Inliner
{
    protected static bool IsInlineableInvocation(
        SemanticModel semanticModel,
        IMethodSymbol contextMethod,
        InvocationExpressionSyntax invocationExpression )
        => invocationExpression.ArgumentList.Arguments.Count == contextMethod.Parameters.Length
           && invocationExpression.ArgumentList.Arguments
               .Select( ( x, i ) => (Argument: x.Expression, Index: i) )
               .All( a => SymbolEqualityComparer.Default.Equals( semanticModel.GetSymbolInfo( a.Argument ).Symbol, contextMethod.Parameters[a.Index] ) );

    public override bool IsValidForTargetSymbol( ISymbol symbol )
        => symbol is IMethodSymbol { MethodKind: not MethodKind.Constructor, AssociatedSymbol: null, IsAsync: false } methodSymbol
           && !methodSymbol.IsIteratorMethod();

    public override bool IsValidForContainingSymbol( ISymbol symbol ) => true;
}