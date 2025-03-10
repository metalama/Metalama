// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed class RewriterHelper
{
    private readonly ClassifyingCompilationContext _runTimeCompilationContext;
    private readonly Func<SyntaxNode, SyntaxNode> _rewriteThrowNotSupported;

    public RewriterHelper(
        ClassifyingCompilationContext runTimeCompilationContext,
        Func<SyntaxNode, SyntaxNode>? rewriteThrowNotSupported = null )
    {
        this._runTimeCompilationContext = runTimeCompilationContext;
        this._rewriteThrowNotSupported = rewriteThrowNotSupported ?? (node => node);
    }

    public SemanticModelProvider SemanticModelProvider => this._runTimeCompilationContext.SemanticModelProvider;

    public ISymbolClassifier SymbolClassifier => this._runTimeCompilationContext.SymbolClassifier;

    private T RewriteThrowNotSupported<T>( T node )
        where T : SyntaxNode
        => (T) this._rewriteThrowNotSupported( node );

    private static T WithSuppressedDiagnostics<T>( T member, SyntaxGenerationContext context, params string[] suppressedDiagnostics )
        where T : MemberDeclarationSyntax
    {
        if ( suppressedDiagnostics.Length == 0 )
        {
            return member;
        }

        switch ( member )
        {
            case MethodDeclarationSyntax { Body: { } } method:
                return (T) (MemberDeclarationSyntax) method
                    .WithLeadingTrivia(
                        method
                            .GetLeadingTrivia()
                            .Add( Trivia( GetPragmaTrivia( true ) ) ) )
                    .WithTrailingTrivia(
                        TriviaList( Trivia( GetPragmaTrivia( false ) ) )
                            .AddRange( method.GetTrailingTrivia() ) );

            case MethodDeclarationSyntax { ExpressionBody: { } } method:
                return (T) (MemberDeclarationSyntax) method
                    .WithLeadingTrivia(
                        method
                            .GetLeadingTrivia()
                            .Add( Trivia( GetPragmaTrivia( true ) ) ) )
                    .WithTrailingTrivia(
                        TriviaList( Trivia( GetPragmaTrivia( false ) ) )
                            .AddRange( method.GetTrailingTrivia() ) );

            case MethodDeclarationSyntax method:
                return (T) (MemberDeclarationSyntax) method
                    .WithLeadingTrivia(
                        method
                            .GetLeadingTrivia()
                            .Add( Trivia( GetPragmaTrivia( true ) ) ) )
                    .WithTrailingTrivia(
                        TriviaList( Trivia( GetPragmaTrivia( false ) ) )
                            .AddRange( method.GetTrailingTrivia() ) );

            default:
                throw new AssertionFailedException( $"Unexpected member syntax kind {member.Kind()} at '{member.GetLocation()}'." );
        }

        StructuredTriviaSyntax GetPragmaTrivia( bool disable )
        {
            return PragmaWarningDirectiveTrivia(
                Token( SyntaxKind.HashToken ).WithRequiredLeadingLineFeed( context ),
                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PragmaKeyword ),
                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.WarningKeyword ),
                disable
                    ? SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.DisableKeyword )
                    : SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.RestoreKeyword ),
                SeparatedList<ExpressionSyntax>( suppressedDiagnostics.SelectAsReadOnlyList( IdentifierName ) ),
                Token( SyntaxKind.EndOfDirectiveToken ).WithRequiredTrailingLineFeed( context ),
                true );
        }
    }

    public MethodDeclarationSyntax WithThrowNotSupportedExceptionBody( MethodDeclarationSyntax method, string message, SyntaxGenerationContext context )
    {
        // Method does not have a body (e.g. because it's abstract), so there is nothing to replace.
        if ( method.Modifiers.Any( x => x.IsKind( SyntaxKind.AbstractKeyword ) ) )
        {
            throw new ArgumentException( "Should not be called with a method without a body.", nameof(method) );
        }

        // Otherwise we need to preserve "asyncness" and "iteratorness" of the method.

        var isAsync = method.Modifiers.Any( x => x.IsKind( SyntaxKind.AsyncKeyword ) );
        var isIterator = IteratorHelper.IsIteratorMethod( method );

        var suppressedWarnings = new List<string>();

        if ( isAsync )
        {
            // Throwing async method does not have an await.
            suppressedWarnings.Add( "CS1998" );
        }

        if ( isIterator )
        {
            // Throwing iterator has unreachable yield break.
            suppressedWarnings.Add( "CS0162" );
        }

        return
            this.RewriteThrowNotSupported(
                WithSuppressedDiagnostics(
                    method
                        .WithBody(
                            isIterator
                                ? context.SyntaxGenerator.FormattedBlock(
                                    ThrowStatement( GetNotSupportedExceptionExpression( message ).Expression ),
                                    YieldStatement( SyntaxKind.YieldBreakStatement ) )
                                : null )
                        .WithExpressionBody(
                            isIterator
                                ? null
                                : ArrowExpressionClause( GetNotSupportedExceptionExpression( message ) ) )
                        .WithModifiers( TokenList( method.Modifiers.Where( m => !m.IsKind( SyntaxKind.ExternKeyword ) ) ) )
                        .WithSemicolonToken( isIterator ? default : Token( SyntaxKind.SemicolonToken ) )
                        .NormalizeWhitespace( eol: EndOfLineHelper.DetermineEndOfLineStyleFast( method.SyntaxTree ) )
                        .WithLeadingTrivia( method.GetLeadingTrivia() )
                        .WithTrailingTrivia( context.TwoElasticEndOfLinesTriviaList ),
                    context,
                    suppressedWarnings.ToArray() ) );
    }

    public BasePropertyDeclarationSyntax WithThrowNotSupportedExceptionBody( BasePropertyDeclarationSyntax memberDeclaration, string message )
    {
        if ( memberDeclaration.Modifiers.Any( x => x.IsKind( SyntaxKind.AbstractKeyword ) ) )
        {
            // Should not be called with an abstract property.
            throw new ArgumentOutOfRangeException( nameof(memberDeclaration) );
        }

        switch ( memberDeclaration )
        {
            case PropertyDeclarationSyntax { ExpressionBody: { } } property:
                // Expression bodied property - change the expression to throw exception.
                return this.RewriteThrowNotSupported(
                    property
                        .WithExpressionBody( property.ExpressionBody?.WithExpression( GetNotSupportedExceptionExpression( message ) ) )
                        .WithModifiers( TokenList( property.Modifiers.Where( m => !m.IsKind( SyntaxKind.ExternKeyword ) ) ) )
                        .NormalizeWhitespace( eol: EndOfLineHelper.DetermineEndOfLineStyleFast( property.SyntaxTree ) )
                        .WithLeadingTrivia( property.GetLeadingTrivia() )
                        .WithTrailingTrivia( LineFeed, LineFeed ) );

            case PropertyDeclarationSyntax { AccessorList: { } } property:
                // Property with accessor list - change all accessors to expression bodied which do throw exception, remove initializer.

                return this.RewriteThrowNotSupported(
                    property
                        .WithAccessorList(
                            property.AccessorList!.WithAccessors(
                                List(
                                    property.AccessorList.Accessors.SelectAsReadOnlyList(
                                        x => x
                                            .WithBody( null )
                                            .WithExpressionBody( ArrowExpressionClause( GetNotSupportedExceptionExpression( message ) ) )
                                            .WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) ) ) ) ) )
                        .WithInitializer( null )
                        .WithModifiers( TokenList( property.Modifiers.Where( m => !m.IsKind( SyntaxKind.ExternKeyword ) ) ) )
                        .NormalizeWhitespace( eol: EndOfLineHelper.DetermineEndOfLineStyleFast( property.SyntaxTree ) )
                        .WithLeadingTrivia( property.GetLeadingTrivia() )
                        .WithTrailingTrivia( LineFeed, LineFeed ) );

            case IndexerDeclarationSyntax { AccessorList: { } }:
                throw new AssertionFailedException( "Build-time indexers are not supported." );
            /*
            // Property with accessor list - change all accessors to expression bodied which do throw exception, remove initializer.

            return indexer
                .WithAccessorList(
                    indexer.AccessorList!.WithAccessors(
                        List(
                            indexer.AccessorList.Accessors.Select(
                                x => x
                                    .WithBody( null )
                                    .WithExpressionBody( ArrowExpressionClause( GetNotSupportedExceptionExpression( message ) ) )
                                    .WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) ) ) ) ) )
                .NormalizeWhitespace( eol: EndOfLineHelper.DetermineEndOfLineStyleFast( property.SyntaxTree ) )
                .WithLeadingTrivia( indexer.GetLeadingTrivia() )
                .WithTrailingTrivia( LineFeed, LineFeed );
                */

            case EventDeclarationSyntax @event:
                // Event with accessor list.

                return this.RewriteThrowNotSupported(
                    @event
                        .WithAccessorList(
                            @event.AccessorList.AssertNotNull()
                                .WithAccessors(
                                    List(
                                        @event.AccessorList.AssertNotNull()
                                            .Accessors.SelectAsReadOnlyList(
                                                x => x
                                                    .WithBody( null )
                                                    .WithExpressionBody( ArrowExpressionClause( GetNotSupportedExceptionExpression( message ) ) )
                                                    .WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) ) ) ) ) )
                        .WithModifiers( TokenList( @event.Modifiers.Where( m => !m.IsKind( SyntaxKind.ExternKeyword ) ) ) )
                        .NormalizeWhitespace( eol: EndOfLineHelper.DetermineEndOfLineStyleFast( @event.SyntaxTree ) )
                        .WithLeadingTrivia( @event.GetLeadingTrivia() )
                        .WithTrailingTrivia( LineFeed, LineFeed ) );

            default:
                throw new AssertionFailedException( $"Unexpected declaration syntax kind {memberDeclaration.Kind()} at '{memberDeclaration.GetLocation()}'." );
        }
    }

    /*
    protected static IEnumerable<EventDeclarationSyntax> WithThrowNotSupportedExceptionBody( EventFieldDeclarationSyntax @eventField, string message )
    {
        // Event with accessor list.

        foreach ( var declarator in @eventField.Declaration.Variables )
        {
            yield return
                EventDeclaration(
                    List<AttributeListSyntax>(),
                    eventField.Modifiers,
                    eventField.Declaration.Type,
                    null,
                    declarator.Identifier,
                    AccessorList(
                        List(
                            new[]
                            {
                                AccessorDeclaration(
                                    SyntaxKind.AddAccessorDeclaration,
                                    List<AttributeListSyntax>(),
                                    TokenList(),
                                    ArrowExpressionClause( GetNotSupportedExceptionExpression( message ) ) ),
                                AccessorDeclaration(
                                    SyntaxKind.RemoveAccessorDeclaration,
                                    List<AttributeListSyntax>(),
                                    TokenList(),
                                    ArrowExpressionClause( GetNotSupportedExceptionExpression( message ) ) )
                            } ) ) );
        }
    }
    */

    private static ThrowExpressionSyntax GetNotSupportedExceptionExpression( string message )
        =>

            // throw new System.NotSupportedException("message")
            ThrowExpression(
                ObjectCreationExpression( ParseTypeName( "System.NotSupportedException" ) )
                    .AddArgumentListArguments( Argument( LiteralExpression( SyntaxKind.StringLiteralExpression, Literal( message ) ) ) ) );
}