// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Microsoft.CodeAnalysis.Editing;

namespace Metalama.Framework.Engine.Analyzers;

[ExportCodeFixProvider( LanguageNames.CSharp )]
[UsedImplicitly]
public class MetalamaPerformanceCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create( MetalamaPerformanceAnalyzer.NormalizeWhitespace.Id, MetalamaPerformanceAnalyzer.SyntaxNodeWith.Id );

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync( CodeFixContext context )
    {
        foreach ( var diagnostic in context.Diagnostics )
        {
            if ( diagnostic.Id == MetalamaPerformanceAnalyzer.NormalizeWhitespace.Id )
            {
                await RegisterNormalizeWhitespaceCodeFixAsync( context, diagnostic );
            }
            else if ( diagnostic.Id == MetalamaPerformanceAnalyzer.SyntaxNodeWith.Id )
            {
                await RegisterSyntaxNodeWithCodeFixAsync( context, diagnostic );
            }
        }
    }

    private static async Task RegisterNormalizeWhitespaceCodeFixAsync( CodeFixContext context, Diagnostic diagnostic )
    {
        var document = context.Document;
        var root = await document.GetSyntaxRootAsync( context.CancellationToken );
        var node = root.FindNode( diagnostic.Location.SourceSpan );

        if ( node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: var expression } } )
        {
            const string title = "Remove call to NormalizeWhitespace";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    _ =>
                    {
                        var newRoot = root.ReplaceNode( node, expression.WithTrailingTrivia( node.GetTrailingTrivia() ) );

                        return Task.FromResult( document.WithSyntaxRoot( newRoot ) );
                    },
                    equivalenceKey: title ),
                diagnostic );
        }
    }

    private static async Task RegisterSyntaxNodeWithCodeFixAsync( CodeFixContext context, Diagnostic diagnostic )
    {
        var document = context.Document;
        var root = await document.GetSyntaxRootAsync( context.CancellationToken );
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode( diagnosticSpan ) as ExpressionSyntax;
        var generator = SyntaxGenerator.GetGenerator( document );
        var semanticModel = await document.GetSemanticModelAsync( context.CancellationToken );

        List<ArgumentSyntax> arguments = [];

        var currentNode = node;
        MemberAccessExpressionSyntax? firstMemberAccess = null;

        while ( currentNode.Span.OverlapsWith( diagnosticSpan ) && currentNode is InvocationExpressionSyntax
               {
                   Expression: MemberAccessExpressionSyntax { Expression: var expression, Name: IdentifierNameSyntax name } memberAccess,
                   ArgumentList.Arguments: [var argument]
               } )
        {
            firstMemberAccess ??= memberAccess;

            var withMethodName = name.Identifier.ValueText;
            var trimmedMethodName = withMethodName["With".Length..];
            var parameterName = char.ToLowerInvariant( trimmedMethodName[0] ) + trimmedMethodName[1..];

            var argumentExpression = argument.Expression;

            if ( argumentExpression.IsKind( SyntaxKind.DefaultLiteralExpression ) )
            {
                // WithFoo(default) means reset Foo, but PartialUpdate(foo: default) means don't change Foo.
                // So we need to instead use PartialUpdate(foo: default(typeof(foo))).

                var withMethodSymbol = (IMethodSymbol) semanticModel.GetSymbolInfo( memberAccess ).Symbol;

                argumentExpression = (ExpressionSyntax) generator.DefaultExpression( withMethodSymbol.Parameters[0].Type );
            }

            arguments.Add( SyntaxFactory.Argument( SyntaxFactory.NameColon( parameterName ), default, argumentExpression ) );

            currentNode = expression;
        }

        if ( arguments.Count >= 2 )
        {
            const string title = "Replace With calls with PartialUpdate";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    _ =>
                    {
                        var memberAccess = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            currentNode,
                            SyntaxFactory.Token( firstMemberAccess!.OperatorToken.LeadingTrivia, SyntaxKind.DotToken, default ),
                            SyntaxFactory.IdentifierName( "PartialUpdate" ) );

                        var newNode = SyntaxFactory.InvocationExpression(
                                memberAccess,
                                SyntaxFactory.ArgumentList( SyntaxFactory.SeparatedList( arguments.AsEnumerable().Reverse() ) ) )
                            .WithTrailingTrivia( node.GetTrailingTrivia() );

                        var newRoot = root.ReplaceNode( node, newNode );

                        return Task.FromResult( document.WithSyntaxRoot( newRoot ) );
                    },
                    equivalenceKey: title ),
                diagnostic );
        }
    }
}