// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Extensibility.CallSites;
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
/// Describes a requested rewrite of a source call site. All the syntax is computed when the request is validated, so the injection rewriter builds
/// the final call from syntax only.
/// </summary>
internal sealed class CallSiteRedirection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallSiteRedirection"/> class.
    /// </summary>
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

    /// <summary>
    /// Gets a value indicating whether the redirection rewrites an invocation or a method reference.
    /// </summary>
    public CallSiteRedirectionKind Kind { get; }

    /// <summary>
    /// Gets the mode that determines how the new call handles the receiver of the source call site.
    /// </summary>
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

    /// <summary>
    /// Gets a human-readable description of the redirection, which is used in the diagnostic reported when the redirection is not applied.
    /// </summary>
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
                    RedirectedArgumentKind.SourceArgument when item.IsPacked => Argument( CreatePackedCollection( item, sourceArguments ) ),
                    RedirectedArgumentKind.SourceArgument => sourceArguments[item.SourceArgumentIndex],
                    _ => Argument( item.Value! )
                };

                if ( item.CastType != null )
                {
                    // The factory accepts a cast only on an argument that is passed by value, so the argument has no modifier.
                    argument = Argument( CastExpression( item.CastType, ParenthesizedExpression( argument.Expression.WithoutTrivia() ) ) );
                }

                if ( !item.PrecedingDiscards.IsDefaultOrEmpty || !item.FollowingDiscards.IsDefaultOrEmpty )
                {
                    // The factory attaches dropped values only to an argument that is passed by value, so the argument has no modifier.
                    argument = Argument( AddDiscards( argument.Expression, item, sourceArguments ) );
                }

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

                // The receiver of p->M() is the variable *p, so the extension method is called on (*p).
                SyntaxKind.PointerMemberAccessExpression => MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ParenthesizedExpression( GetReceiverExpression( invocation.Expression ) ),
                    name ),
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
    /// Packs the elements of an expanded <c>params</c> argument into one collection: <c>[e1, e2]</c>, or <c>new T[] { e1, e2 }</c> before C# 12.
    /// </summary>
    /// <remarks>
    /// The compiler evaluates the elements of an expanded <c>params</c> argument into a collection that it creates at the call site. The packed
    /// collection creates the same collection, with the elements evaluated in the same order, and it can be written as a named argument.
    /// </remarks>
    private static ExpressionSyntax CreatePackedCollection( CallSiteArgumentPlanItem item, SeparatedSyntaxList<ArgumentSyntax> sourceArguments )
    {
        if ( item.PackedElements.IsEmpty )
        {
            return item.PackedEmptyValue!;
        }

        var elements = item.PackedElements.SelectAsArray( i => sourceArguments[i].Expression.WithoutTrivia() );

        if ( item.PackedArrayElementType == null )
        {
            return CollectionExpression( SeparatedList<CollectionElementSyntax>( elements.SelectAsArray( e => (CollectionElementSyntax) ExpressionElement( e ) ) ) );
        }

        return ArrayCreationExpression(
            SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.NewKeyword ),
            ArrayType( item.PackedArrayElementType, SingletonList( ArrayRankSpecifier( SingletonSeparatedList<ExpressionSyntax>( OmittedArraySizeExpression() ) ) ) ),
            InitializerExpression( SyntaxKind.ArrayInitializerExpression, SeparatedList( elements ) ) );
    }

    /// <summary>
    /// Wraps the value of an argument into the calls of <c>Metalama.Framework.RunTime.CallSiteHelper</c> that evaluate the source arguments that the
    /// new call does not pass, in the order of the source call site.
    /// </summary>
    /// <remarks>
    /// C# evaluates the arguments of a call in the order in which they are written. <c>DropBefore( D, value )</c> therefore evaluates <c>D</c>
    /// before the value, and <c>DropAfter( value, D )</c> evaluates it after the value. Several consecutive dropped values are passed as one tuple,
    /// in the source order. When both forms apply, <c>DropBefore( D1, DropAfter( value, D2 ) )</c> evaluates <c>D1</c>, the value and <c>D2</c>.
    /// The type arguments are written when the plan gives them, so that a value without natural type is converted to the same type as in the
    /// original call.
    /// </remarks>
    private static ExpressionSyntax AddDiscards( ExpressionSyntax value, CallSiteArgumentPlanItem item, SeparatedSyntaxList<ArgumentSyntax> sourceArguments )
    {
        var result = value.WithoutTrivia();

        if ( !item.FollowingDiscards.IsDefaultOrEmpty )
        {
            result = CreateDropCall(
                item,
                "DropAfter",
                item.KeepTypeArgument != null ? [item.KeepTypeArgument, item.FollowingDropTypeArgument!] : null,
                result,
                CreateDroppedValue( item.FollowingDiscards, sourceArguments ) );
        }

        if ( !item.PrecedingDiscards.IsDefaultOrEmpty )
        {
            result = CreateDropCall(
                item,
                "DropBefore",
                item.KeepTypeArgument != null ? [item.PrecedingDropTypeArgument!, item.KeepTypeArgument] : null,
                CreateDroppedValue( item.PrecedingDiscards, sourceArguments ),
                result );
        }

        return result;
    }

    /// <summary>
    /// Creates a call of a method of <c>CallSiteHelper</c> with two arguments, in the order of evaluation.
    /// </summary>
    private static InvocationExpressionSyntax CreateDropCall(
        CallSiteArgumentPlanItem item,
        string methodName,
        TypeSyntax[]? typeArguments,
        ExpressionSyntax firstArgument,
        ExpressionSyntax secondArgument )
    {
        SimpleNameSyntax name = typeArguments == null
            ? SyntaxFactoryEx.SafeIdentifierName( methodName )
            : GenericName( SyntaxFactoryEx.SafeIdentifier( methodName ), TypeArgumentList( SeparatedList( typeArguments ) ) );

        return InvocationExpression(
            MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, item.DropHelperType!, name ),
            ArgumentList( SeparatedList( [Argument( firstArgument ), Argument( secondArgument )] ) ) );
    }

    /// <summary>
    /// Creates the expression of the dropped values: the source argument itself when there is one, or a tuple of the source arguments in their
    /// source order when there are several.
    /// </summary>
    private static ExpressionSyntax CreateDroppedValue( ImmutableArray<int> indices, SeparatedSyntaxList<ArgumentSyntax> sourceArguments )
        => indices.Length == 1
            ? sourceArguments[indices[0]].Expression.WithoutTrivia()
            : TupleExpression( SeparatedList( indices.Select( i => Argument( sourceArguments[i].Expression.WithoutTrivia() ) ) ) );

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

    /// <summary>
    /// Determines whether a trivia is a single-line or a multi-line comment.
    /// </summary>
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
