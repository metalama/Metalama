// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Extensibility.Transformations;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        if ( this.ArgumentPlan == null )
        {
            arguments.AddRange( sourceArguments );
        }
        else
        {
            foreach ( var item in this.ArgumentPlan.Value )
            {
                var nameColon = NameColon( SyntaxFactoryEx.SafeIdentifierName( item.Name ) );

                var argument = item.Kind switch
                {
                    RedirectedArgumentKind.SourceReceiver => Argument( GetReceiverExpression( invocation.Expression ) ),
                    RedirectedArgumentKind.SourceArgument => sourceArguments[item.SourceArgumentIndex].WithoutTrivia(),
                    _ => Argument( item.Value! )
                };

                arguments.Add( argument.WithNameColon( nameColon ) );
            }
        }

        arguments.AddRange( this.ExtraArguments );

        var argumentList = invocation.ArgumentList.WithArguments( SeparatedList( arguments ) );

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

            result = invocation.PartialUpdate( expression: expression, argumentList: argumentList );
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
    /// Gives the rewritten node the trivia of the source node, which can contain comments and preprocessor directives.
    /// </summary>
    private static ExpressionSyntax WithTriviaOf( ExpressionSyntax node, SyntaxNode source )
        => node.WithRequiredLeadingTrivia( source.GetLeadingTrivia() ).WithRequiredTrailingTrivia( source.GetTrailingTrivia() );

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
