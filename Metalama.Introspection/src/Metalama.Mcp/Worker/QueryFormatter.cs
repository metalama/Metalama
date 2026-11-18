// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text;

namespace Metalama.Mcp.Worker;

/// <summary>
/// Lays out the query shown in the editor of a served page. A long query written on one line, as agents often send them,
/// gets one call of its fluent chain per line; a query already written on several lines is left as it is.
/// </summary>
internal static class QueryFormatter
{
    private const int _maxSingleLineLength = 60;
    private const string _indent = "    ";

    public static string Format( string code )
    {
        var trimmed = code.Trim();

        if ( trimmed.Contains( '\n' ) || trimmed.Length <= _maxSingleLineLength || !QueryCompiler.TryGetExpression( trimmed, out var expressionText ) )
        {
            return trimmed;
        }

        var expression = SyntaxFactory.ParseExpression( expressionText );

        // The calls of the chain, from the last to the first: x.A().B().C() gives C, B, A.
        var calls = new List<(MemberAccessExpressionSyntax Access, InvocationExpressionSyntax Invocation)>();
        var current = expression;

        while ( current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } invocation )
        {
            calls.Add( (access, invocation) );
            current = access.Expression;
        }

        if ( calls.Count < 2 )
        {
            return trimmed;
        }

        calls.Reverse();

        var builder = new StringBuilder();
        // The receiver of the first call stays on the first line: `workspace.SourceCode.Types`.
        builder.Append( expressionText, 0, calls[0].Access.OperatorToken.SpanStart );
        TrimEnd( builder );

        foreach ( var (access, invocation) in calls )
        {
            builder.Append( '\n' ).Append( _indent );
            builder.Append( expressionText, access.OperatorToken.SpanStart, invocation.Span.End - access.OperatorToken.SpanStart );
        }

        // What follows the chain, such as the semicolon of `x.A().B();`.
        builder.Append( trimmed, expression.Span.End, trimmed.Length - expression.Span.End );

        return builder.ToString();
    }

    private static void TrimEnd( StringBuilder builder )
    {
        while ( builder.Length > 0 && char.IsWhiteSpace( builder[^1] ) )
        {
            builder.Length--;
        }
    }
}
