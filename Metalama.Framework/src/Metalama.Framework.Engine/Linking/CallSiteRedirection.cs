// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Extensibility.Transformations;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// The kinds of <see cref="CallSiteRedirection"/>.
/// </summary>
internal enum CallSiteRedirectionKind
{
    Invocation,
    MethodReference
}

/// <summary>
/// One element of the argument list of a redirected invocation, in the order in which it is written.
/// </summary>
/// <param name="Kind">The kind of the argument.</param>
/// <param name="SourceArgumentIndex">For <see cref="RedirectedArgumentKind.SourceArgument"/>, the index of the argument in the argument list of the
/// source invocation.</param>
/// <param name="Value">For <see cref="RedirectedArgumentKind.Value"/>, the expression of the argument.</param>
/// <param name="Name">The parameter name with which the argument is written.</param>
internal readonly record struct CallSiteArgumentPlanItem( RedirectedArgumentKind Kind, int SourceArgumentIndex, ExpressionSyntax? Value, string Name );

/// <summary>
/// Describes a requested rewrite of a source call site. All the syntax is computed when the request is validated, so the injection rewriter builds
/// the final call from syntax only.
/// </summary>
internal sealed class CallSiteRedirection
{
    public CallSiteRedirection(
        int id,
        ExpressionSyntax sourceNode,
        CallSiteRedirectionKind kind,
        CallSiteReceiverMode receiverMode,
        ExpressionSyntax callee,
        ImmutableArray<CallSiteArgumentPlanItem>? argumentPlan,
        ImmutableArray<ArgumentSyntax> extraArguments,
        TypeSyntax? resultCast,
        string description )
    {
        this.Id = id;
        this.SourceNode = sourceNode;
        this.Kind = kind;
        this.ReceiverMode = receiverMode;
        this.Callee = callee;
        this.ArgumentPlan = argumentPlan;
        this.ExtraArguments = extraArguments;
        this.ResultCast = resultCast;
        this.Description = description;
    }

    /// <summary>
    /// Gets the sequential identifier of the redirection, which orders the diagnostics of the completeness check.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the source node: an <see cref="InvocationExpressionSyntax"/>, or the expression of a method group.
    /// </summary>
    public ExpressionSyntax SourceNode { get; }

    public CallSiteRedirectionKind Kind { get; }

    public CallSiteReceiverMode ReceiverMode { get; }

    /// <summary>
    /// Gets the expression that designates the new target: the qualified name of a static method, or the simple name of an extension method for
    /// <see cref="CallSiteReceiverMode.ExtensionReceiver"/>.
    /// </summary>
    public ExpressionSyntax Callee { get; }

    /// <summary>
    /// Gets the argument list of the new call after the receiver, in the order in which it is written, or <c>null</c> to keep the arguments of the
    /// source call site.
    /// </summary>
    public ImmutableArray<CallSiteArgumentPlanItem>? ArgumentPlan { get; }

    /// <summary>
    /// Gets the named arguments appended to the new call.
    /// </summary>
    public ImmutableArray<ArgumentSyntax> ExtraArguments { get; }

    /// <summary>
    /// Gets the type to which the result of the new call is cast, or <c>null</c>.
    /// </summary>
    public TypeSyntax? ResultCast { get; }

    public string Description { get; }

    /// <summary>
    /// Builds the final syntax of the call site.
    /// </summary>
    /// <param name="visitedNode">The source node after the injection rewriter has visited its children, so that nested redirections are already
    /// applied.</param>
    public ExpressionSyntax Rewrite( ExpressionSyntax visitedNode )
    {
        if ( this.Kind == CallSiteRedirectionKind.MethodReference )
        {
            return WithTriviaOf( this.Callee, visitedNode );
        }

        var invocation = (InvocationExpressionSyntax) visitedNode;
        var sourceArguments = invocation.ArgumentList.Arguments;
        var arguments = new List<ArgumentSyntax>( sourceArguments.Count + this.ExtraArguments.Length + 1 );

        // The receiver is passed positionally, before the other arguments, by the FirstArgument modes.
        var receiverArgument = this.ReceiverMode switch
        {
            CallSiteReceiverMode.FirstArgument => Argument( GetReceiverExpression( invocation.Expression ) ),
            CallSiteReceiverMode.FirstArgumentByRef => Argument( null, SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.RefKeyword ), GetReceiverExpression( invocation.Expression ) ),
            CallSiteReceiverMode.FirstArgumentByIn => Argument( null, SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.InKeyword ), GetReceiverExpression( invocation.Expression ) ),
            _ => null
        };

        if ( receiverArgument != null )
        {
            arguments.Add( receiverArgument );
        }

        SeparatedSyntaxList<ArgumentSyntax> newArguments;

        if ( this.ArgumentPlan == null )
        {
            // The source arguments are kept with their separators, so that the trivia of the separators is kept too.
            var nodesAndTokens = new List<SyntaxNodeOrToken>( (2 * arguments.Count) + (2 * this.ExtraArguments.Length) + sourceArguments.SeparatorCount + sourceArguments.Count + 1 );

            void AddSeparatorIfNecessary()
            {
                if ( nodesAndTokens.Count > 0 )
                {
                    nodesAndTokens.Add( Token( SyntaxKind.CommaToken ) );
                }
            }

            foreach ( var argument in arguments )
            {
                AddSeparatorIfNecessary();
                nodesAndTokens.Add( argument );
            }

            if ( sourceArguments.Count > 0 )
            {
                AddSeparatorIfNecessary();
                nodesAndTokens.AddRange( sourceArguments.GetWithSeparators() );
            }

            foreach ( var argument in this.ExtraArguments )
            {
                AddSeparatorIfNecessary();
                nodesAndTokens.Add( argument );
            }

            newArguments = SeparatedList<ArgumentSyntax>( nodesAndTokens );
        }
        else
        {
            foreach ( var item in this.ArgumentPlan.Value )
            {
                var nameColon = NameColon( SyntaxFactoryEx.SafeIdentifierName( item.Name ) );

                var argument = item.Kind switch
                {
                    RedirectedArgumentKind.SourceReceiver => Argument( GetReceiverExpression( invocation.Expression ) ),
                    RedirectedArgumentKind.SourceArgument => sourceArguments[item.SourceArgumentIndex],
                    _ => Argument( item.Value! )
                };

                arguments.Add( argument.WithNameColon( nameColon ) );
            }

            arguments.AddRange( this.ExtraArguments );
            newArguments = SeparatedList( arguments );
        }

        var argumentList = invocation.ArgumentList.WithArguments( newArguments );

        ExpressionSyntax result;

        if ( this.ReceiverMode == CallSiteReceiverMode.ExtensionReceiver )
        {
            var name = (SimpleNameSyntax) this.Callee;

            ExpressionSyntax expression = invocation.Expression.Kind() switch
            {
                SyntaxKind.SimpleMemberAccessExpression when invocation.Expression is MemberAccessExpressionSyntax memberAccess => memberAccess.WithName( name ),
                SyntaxKind.MemberBindingExpression when invocation.Expression is MemberBindingExpressionSyntax memberBinding => memberBinding.WithName( name ),
                _ => MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), name )
            };

            // The outer trivia of the source node is restored below, so it is removed here to avoid duplicating it inside a result cast.
            result = invocation.PartialUpdate( expression: expression, argumentList: argumentList ).WithoutTrivia();
        }
        else
        {
            result = InvocationExpression( this.Callee, argumentList );
        }

        if ( this.ResultCast != null )
        {
            result = ParenthesizedExpression( CastExpression( this.ResultCast, ParenthesizedExpression( result ) ) );
        }

        return WithTriviaOf( result, visitedNode );
    }

    /// <summary>
    /// Gives the rewritten node the trivia of the source node, which can contain comments and preprocessor directives, and moves the comments of
    /// the discarded parts of the source node to the leading trivia of the rewritten node.
    /// </summary>
    /// <remarks>
    /// The rewritten node reuses some parts of the source node, for instance the receiver and the arguments, and discards the others, for instance
    /// the name of the source method. A comment of the source node that does not occur in the rewritten node belongs to a discarded part. The
    /// factory refuses a request whose discarded parts contain a preprocessor directive or disabled text, so comments are the only trivia that
    /// must be moved.
    /// </remarks>
    private static ExpressionSyntax WithTriviaOf( ExpressionSyntax node, SyntaxNode source )
    {
        var leadingTrivia = source.GetLeadingTrivia().AddRange( GetDiscardedComments( source, node ) );

        return node.WithRequiredLeadingTrivia( leadingTrivia ).WithRequiredTrailingTrivia( source.GetTrailingTrivia() );
    }

    /// <summary>
    /// Returns the comments of the inner trivia of a source node that do not occur in the inner trivia of the rewritten node, in source order.
    /// Each comment is followed by the trivia that separates it from the next token.
    /// </summary>
    private static IEnumerable<SyntaxTrivia> GetDiscardedComments( SyntaxNode source, SyntaxNode rewritten )
    {
        // Comments are compared by text, because the rewritten node contains copies of the trivia of the reused parts.
        var keptComments = new Dictionary<string, int>( StringComparer.Ordinal );

        foreach ( var trivia in GetInnerTriviaLists( rewritten ).SelectMany( l => l ) )
        {
            if ( IsComment( trivia ) )
            {
                var text = trivia.ToString();
                keptComments.TryGetValue( text, out var count );
                keptComments[text] = count + 1;
            }
        }

        foreach ( var triviaList in GetInnerTriviaLists( source ) )
        {
            for ( var i = 0; i < triviaList.Count; i++ )
            {
                var trivia = triviaList[i];

                if ( !IsComment( trivia ) )
                {
                    continue;
                }

                var text = trivia.ToString();

                if ( keptComments.TryGetValue( text, out var count ) && count > 0 )
                {
                    keptComments[text] = count - 1;

                    continue;
                }

                yield return trivia;

                if ( trivia.IsKind( SyntaxKind.SingleLineCommentTrivia ) )
                {
                    // A single-line comment must be followed by a line break. In the source, the line break is the next trivia.
                    yield return i + 1 < triviaList.Count && triviaList[i + 1].IsKind( SyntaxKind.EndOfLineTrivia ) ? triviaList[i + 1] : LineFeed;
                }
                else
                {
                    yield return Space;
                }
            }
        }
    }

    private static bool IsComment( SyntaxTrivia trivia ) => trivia.IsKind( SyntaxKind.SingleLineCommentTrivia ) || trivia.IsKind( SyntaxKind.MultiLineCommentTrivia );

    /// <summary>
    /// Returns the trivia lists of the tokens of a node, except the leading trivia of the first token and the trailing trivia of the last token of
    /// an outer node, which the rewrite keeps.
    /// </summary>
    /// <param name="outerNode">The node whose first and last tokens delimit the inner trivia.</param>
    /// <param name="node">The node whose tokens are enumerated. It is <paramref name="outerNode"/> or one of its descendants. The default value is
    /// <paramref name="outerNode"/>.</param>
    internal static IEnumerable<SyntaxTriviaList> GetInnerTriviaLists( SyntaxNode outerNode, SyntaxNode? node = null )
    {
        var firstToken = outerNode.GetFirstToken();
        var lastToken = outerNode.GetLastToken();

        foreach ( var token in (node ?? outerNode).DescendantTokens() )
        {
            if ( token != firstToken )
            {
                yield return token.LeadingTrivia;
            }

            if ( token != lastToken )
            {
                yield return token.TrailingTrivia;
            }
        }
    }

    /// <summary>
    /// Determines whether the inner trivia of a node contains a preprocessor directive or disabled text.
    /// </summary>
    /// <param name="outerNode">The node whose first and last tokens delimit the inner trivia.</param>
    /// <param name="node">The node whose tokens are examined. It is <paramref name="outerNode"/> or one of its descendants. The default value is
    /// <paramref name="outerNode"/>.</param>
    internal static bool HasDirectiveInInnerTrivia( SyntaxNode outerNode, SyntaxNode? node = null )
        => (node ?? outerNode).ContainsDirectives
           && GetInnerTriviaLists( outerNode, node ).Any( l => l.Any( t => t.IsDirective || t.IsKind( SyntaxKind.DisabledTextTrivia ) ) );

    /// <summary>
    /// Returns the expression that passes the receiver of the source call as an argument: the receiver of a member access, <c>this</c> for an
    /// implicit receiver or for <c>base</c>, and <c>*p</c> for <c>p-&gt;M()</c>.
    /// </summary>
    private static ExpressionSyntax GetReceiverExpression( ExpressionSyntax invokedExpression )
        => invokedExpression.Kind() switch
        {
            SyntaxKind.PointerMemberAccessExpression when invokedExpression is MemberAccessExpressionSyntax pointerAccess
                => PrefixUnaryExpression( SyntaxKind.PointerIndirectionExpression, ParenthesizedExpression( pointerAccess.Expression.WithoutTrivia() ) ),
            SyntaxKind.SimpleMemberAccessExpression when invokedExpression is MemberAccessExpressionSyntax memberAccess
                => memberAccess.Expression.Kind() == SyntaxKind.BaseExpression ? ThisExpression() : memberAccess.Expression.WithoutTrivia(),
            _ => ThisExpression()
        };
}
