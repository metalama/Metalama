// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Templating;

/// <summary>
/// A <see cref="CSharpSyntaxRewriter"/> that transforms a syntax tree
/// into another syntax tree (typically an expression) that, when executed, returns the original syntax tree.
/// Parts of the tree may be transformed (typically run-time code), part may be preserved
/// (typically compile-time code), according to the result of the <see cref="GetTransformationKind"/>
/// method.
/// </summary>
/// <remarks>
/// Most of this class is machine-generated. This class is meant to be inherited. See the only
/// inheritor: <see cref="TemplateCompilerRewriter"/>.
/// </remarks>
internal partial class MetaSyntaxRewriter : SafeSyntaxRewriter
{
    private readonly Stack<string> _indentTriviaStack = new();
    private readonly IndentRewriter _indentRewriter;
    private readonly SyntaxGenerationOptions _generationOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetaSyntaxRewriter"/> class.
    /// </summary>
    /// <param name="compileTimeCompilation">The <see cref="Compilation"/> used to create the compile-time assembly,
    /// possibly with no source code, but with metadata references. Used to resolve symbols in the compile-time assembly.</param>
    public MetaSyntaxRewriter( CompilationContext compileTimeCompilation, RoslynApiVersion targetApiVersion )
    {
        this.TargetApiVersion = targetApiVersion;
        this._indentTriviaStack.Push( "" );
        this._indentRewriter = new IndentRewriter( this );
        this.MetaSyntaxFactory = new MetaSyntaxFactoryImpl( compileTimeCompilation );
        this._generationOptions = this.MetaSyntaxFactory.SyntaxGenerationContext.Options;
    }

    protected MetaSyntaxFactoryImpl MetaSyntaxFactory { get; }

    // ReSharper disable UnusedAutoPropertyAccessor.Local
    private RoslynApiVersion TargetApiVersion { get; }

    /// <summary>
    /// Determines how a given <see cref="SyntaxNode"/> must be transformed.
    /// </summary>
    protected virtual TransformationKind GetTransformationKind( SyntaxNode node ) => TransformationKind.Transform;

    protected void Indent( int level = 1 )
    {
        for ( var i = 0; i < level; i++ )
        {
            // TODO: optimize to avoid string allocation.
            this._indentTriviaStack.Push( this._indentTriviaStack.Peek() + "    " );
        }
    }

    protected void Unindent( int level = 1 )
    {
        for ( var i = 0; i < level; i++ )
        {
            this._indentTriviaStack.Pop();
        }
    }

    protected SyntaxTriviaList GetIndentation( bool lineFeed = true )
        => lineFeed
#if ROSLYN_4_12_0_OR_GREATER

            // Using the new Create(ReadOnlySpan) avoids array allocation.
            ? SyntaxTriviaList.Create( [this.MetaSyntaxFactory.SyntaxGenerationContext.ElasticEndOfLineTrivia, Whitespace( this._indentTriviaStack.Peek() )] )
#else
            ? new SyntaxTriviaList(this.MetaSyntaxFactory.SyntaxGenerationContext.ElasticEndOfLineTrivia, Whitespace( this._indentTriviaStack.Peek() ))
#endif
            : SyntaxTriviaList.Create( Whitespace( this._indentTriviaStack.Peek() ) );

    protected static SyntaxTriviaList GetLineBreak() => SyntaxTriviaList.Empty;

    /// <summary>
    /// Adds indentation to a <see cref="SyntaxNode"/> and all its children.
    /// </summary>
    protected T DeepIndent<T>( T node )
        where T : SyntaxNode
    {
        node = (T) this._indentRewriter.Visit( node )!;

        return node;
    }

    protected virtual ExpressionSyntax TransformExpression( ExpressionSyntax expression ) => expression;

    protected virtual ExpressionSyntax TransformStatement( StatementSyntax statement ) => throw new NotSupportedException();

    /// <summary>
    /// Transforms an put <see cref="SyntaxNode"/> into an output <see cref="ExpressionSyntax"/> instantiating the input <see cref="SyntaxNode"/>,
    /// irrespective of the <see cref="TransformationKind"/> returned by <see cref="GetTransformationKind"/>.
    /// </summary>
    protected ExpressionSyntax Transform<T>( T? node )
        where T : SyntaxNode?
    {
        if ( node == null )
        {
            return LiteralExpression( SyntaxKind.NullLiteralExpression );
        }

        if ( this.GetTransformationKind( node ) != TransformationKind.Transform )
        {
            // GetTransformationKind would not transform the node, so we have to call the transform method manually.
            // We must call Visit on the node. Visit will not transform the node itself, but will apply transformations
            // to children nodes.

            switch ( node )
            {
                case ExpressionSyntax expression:
                    return this.TransformExpression( (ExpressionSyntax) this.Visit( expression )! );

                case ArgumentSyntax argument:
                    return this.TransformArgument( argument );

                case StatementSyntax statement:
                    return this.TransformStatement( statement );

                default:
                    throw new AssertionFailedException( $"Unexpected node kind: {node.Kind()}." );
            }
        }
        else
        {
            return (ExpressionSyntax) this.Visit( node )!;
        }
    }

    protected ExpressionSyntax Transform( SyntaxKind kind ) => this.MetaSyntaxFactory.Kind( kind );

    [UsedImplicitly]
    protected ExpressionSyntax Transform<T>( SeparatedSyntaxList<T> list )
        where T : SyntaxNode
    {
        if ( list.Count == 0 )
        {
            return DefaultExpression( this.MetaSyntaxFactory.GenericType( typeof(SeparatedSyntaxList<>), this.MetaSyntaxFactory.Type( typeof(T) ) ) );
        }

        if ( list.Count == 1 )
        {
            return this.MetaSyntaxFactory.SingletonSeparatedList<T>( this.Transform( list[0] ) );
        }

        return this.MetaSyntaxFactory.SeparatedList<T>( list.SelectAsReadOnlyList( this.Transform ) );
    }

    [UsedImplicitly]
    protected ExpressionSyntax Transform( BracketedArgumentListSyntax? list )
    {
        if ( list == null )
        {
            return DefaultExpression( this.MetaSyntaxFactory.Type( typeof(BracketedArgumentListSyntax) ) );
        }

        return this.MetaSyntaxFactory.BracketedArgumentList( this.Transform( list.Arguments ) );
    }

    [UsedImplicitly]
    protected ExpressionSyntax Transform( ArgumentListSyntax? list )
        => list == null ? LiteralExpression( SyntaxKind.NullLiteralExpression ) : this.MetaSyntaxFactory.ArgumentList( this.Transform( list.Arguments ) );

    [UsedImplicitly]
    protected ExpressionSyntax Transform( ParameterListSyntax? list )
        => list == null ? LiteralExpression( SyntaxKind.NullLiteralExpression ) : this.MetaSyntaxFactory.ParameterList( this.Transform( list.Parameters ) );

    [UsedImplicitly]
    protected ExpressionSyntax Transform( SyntaxTokenList list )
    {
        if ( list.Count == 0 )
        {
            return DefaultExpression( this.MetaSyntaxFactory.Type( typeof(SyntaxTokenList) ) );
        }

        // TODO: Using default.AddRange is not the right pattern.

        return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    DefaultExpression( this.MetaSyntaxFactory.Type( typeof(SyntaxTokenList) ) ),
                    IdentifierName( "AddRange" ) ) )
            .WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList(
                        Argument(
                            ArrayCreationExpression(
                                Token( SyntaxKind.NewKeyword ).WithTrailingTrivia( ElasticSpace ),
                                this.MetaSyntaxFactory.ArrayType<SyntaxToken>(),
                                InitializerExpression(
                                    SyntaxKind.ArrayInitializerExpression,
                                    SeparatedList( list.SelectAsReadOnlyList( this.Transform ) ) ) ) ) ) ) );
    }

    [UsedImplicitly]
    protected ExpressionSyntax Transform<T>( SyntaxList<T> list )
        where T : SyntaxNode
    {
        if ( list.Count == 0 )
        {
            return DefaultExpression( this.MetaSyntaxFactory.Type( typeof(SyntaxList<T>) ) );
        }

        return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    DefaultExpression( this.MetaSyntaxFactory.Type( typeof(SyntaxList<T>) ) ),
                    IdentifierName( "AddRange" ) ) )
            .WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList(
                        Argument(
                            ArrayCreationExpression(
                                Token( SyntaxKind.NewKeyword ).WithTrailingTrivia( ElasticSpace ),
                                this.MetaSyntaxFactory.ArrayType<T>(),
                                InitializerExpression(
                                    SyntaxKind.ArrayInitializerExpression,
                                    SeparatedList( list.SelectAsReadOnlyList( this.Transform ) ) ) ) ) ) ) );
    }

    protected virtual ExpressionSyntax Transform( SyntaxToken token )
    {
        switch ( token.Kind() )
        {
            case SyntaxKind.None:
                return DefaultExpression( this.MetaSyntaxFactory.Type( typeof(SyntaxToken) ) );

            case SyntaxKind.IdentifierToken:
                var text = SyntaxFactoryEx.LiteralExpression( token.Text );

                if ( token.Text == "_" )
                {
                    return this.MetaSyntaxFactory.Identifier(
                        SyntaxFactoryEx.Default,
                        this.MetaSyntaxFactory.Kind( SyntaxKind.UnderscoreToken ),
                        text,
                        text,
                        SyntaxFactoryEx.Default );
                }
                else
                {
                    return this.MetaSyntaxFactory.Identifier( text );
                }

            case SyntaxKind.CharacterLiteralToken:
            case SyntaxKind.NumericLiteralToken:
            case SyntaxKind.StringLiteralToken:
                return this.MetaSyntaxFactory.Literal( token );
        }

        var defaultToken = Token( token.Kind() );

        if ( defaultToken.Value == token.Value )
        {
            // No argument needed.
            return this.MetaSyntaxFactory.Token( this.Transform( token.Kind() ) );
        }

        // Argument needed.

        /*
         * public static Microsoft.CodeAnalysis.SyntaxToken Token (
         * Microsoft.CodeAnalysis.SyntaxTriviaList leading,
         * Microsoft.CodeAnalysis.CSharp.SyntaxKind kind,
         * string text,
         * string valueText,
         * Microsoft.CodeAnalysis.SyntaxTriviaList trailing);
         */

        return this.MetaSyntaxFactory.Token(
            LiteralExpression( SyntaxKind.DefaultLiteralExpression, Token( SyntaxKind.DefaultKeyword ) ),
            this.Transform( token.Kind() ),
            SyntaxFactoryEx.LiteralExpression( token.Text ),
            SyntaxFactoryEx.LiteralExpression( token.ValueText ),
            LiteralExpression( SyntaxKind.DefaultLiteralExpression, Token( SyntaxKind.DefaultKeyword ) ) );
    }

#pragma warning disable CA1822 // Mark members as static

    // Not static for uniformity with other methods.
    [UsedImplicitly]
    protected ExpressionSyntax Transform( bool value )
    {
        return value
            ? LiteralExpression( SyntaxKind.TrueLiteralExpression )
            : LiteralExpression( SyntaxKind.FalseLiteralExpression );
    }
#pragma warning restore CA1822 // Mark members as static

#if DEBUG
    public override TNode? VisitListElement<TNode>( TNode? node )
        where TNode : class
    {
        // This method does not change the behavior of the base method, but it allows for easier debugging
        // of InvalidCastException.
        var transformedNode = this.Visit( node );

        return (TNode?) transformedNode;
    }
#endif
}