// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

// ReSharper disable MemberCanBePrivate.Global

namespace Metalama.Framework.Engine.Templating
{
    internal partial class MetaSyntaxRewriter
    {
        protected sealed partial class MetaSyntaxFactoryImpl
        {
            public SyntaxGenerationContext SyntaxGenerationContext { get; }

            public MetaSyntaxFactoryImpl( CompilationContext compileTimeCompilation )
            {
                this.ReflectionMapper = compileTimeCompilation.ReflectionMapper;

                // We use a null-aware context because this context is used to generate meta code for the run-time code, which may be null-aware.
                // TODO: We would need one context for each syntax tree if we want to respect EOLs.
                this.SyntaxGenerationContext = compileTimeCompilation.GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted, isNullOblivious: false );
            }

            public ReflectionMapper ReflectionMapper { get; }

            public TypeSyntax Type( Type type ) => this.SyntaxGenerationContext.SyntaxGenerator.TypeSyntax( this.ReflectionMapper.GetTypeSymbol( type ) );

            public TypeSyntax Type( ITypeSymbol type )
                => type switch
                {
                    IArrayTypeSymbol arrayType => this.SyntaxGenerationContext.SyntaxGenerator.ArrayTypeExpression(
                        this.SyntaxGenerationContext.SyntaxGenerator.TypeSyntax( arrayType.ElementType ) ),
                    _ => this.SyntaxGenerationContext.SyntaxGenerator.TypeOrNamespace( type )
                };

            public TypeSyntax GenericType( Type type, params TypeSyntax[] genericParameters )
            {
                var qualifiedName = (QualifiedNameSyntax) this.Type( type );

                return SyntaxFactory.QualifiedName(
                    qualifiedName.Left,
                    ((GenericNameSyntax) qualifiedName.Right).WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList( SyntaxFactory.SeparatedList( genericParameters ) ) ) );
            }

            public MemberAccessExpressionSyntax SyntaxFactoryMethod( string name )
                => SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    this.Type( typeof(SyntaxFactory) ),
                    SyntaxFactory.IdentifierName( name ) );

            public MemberAccessExpressionSyntax GenericSyntaxFactoryMethod( string name, params TypeSyntax[] typeArguments )
                => SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    this.Type( typeof(SyntaxFactory) ),
                    SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier( name ),
                        SyntaxFactory.TypeArgumentList( SyntaxFactory.SeparatedList( typeArguments ) ) ) );

            public ArrayTypeSyntax ArrayType<T>()
            {
                return SyntaxFactory.ArrayType( this.Type( typeof(T) ) )
                    .WithRankSpecifiers(
                        SyntaxFactory.SingletonList(
                            SyntaxFactory.ArrayRankSpecifier(
                                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>( SyntaxFactory.OmittedArraySizeExpression() ) ) ) );
            }

            public ExpressionSyntax Kind( SyntaxKind kind )
                => SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    this.Type( typeof(SyntaxKind) ),
                    SyntaxFactory.IdentifierName( kind.ToString() ) );

            public ExpressionSyntax Literal( ExpressionSyntax expression )
            {
                var result = SyntaxFactory.InvocationExpression( this.SyntaxFactoryMethod( nameof(SyntaxFactory.Literal) ) )
                    .AddArgumentListArguments( SyntaxFactory.Argument( expression ) );

                return result;
            }

            public ExpressionSyntax Literal( SyntaxToken token )
                => SyntaxFactory.InvocationExpression( this.SyntaxFactoryMethod( nameof(SyntaxFactory.Literal) ) )
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument( SyntaxFactoryEx.LiteralExpression( token.Text ) ),
                        SyntaxFactory.Argument( SyntaxFactoryEx.LiteralExpression( token.Value.AssertNotNull(), ObjectDisplayOptions.IncludeTypeSuffix ) ) );

            public ExpressionSyntax Literal( object value )
                => SyntaxFactory.InvocationExpression( this.SyntaxFactoryMethod( nameof(SyntaxFactory.Literal) ) )
                    .AddArgumentListArguments( SyntaxFactory.Argument( SyntaxFactoryEx.LiteralExpression( value ) ) );

            public ExpressionSyntax SingletonSeparatedList<T>( ExpressionSyntax item )
                where T : SyntaxNode
            {
                var itemType = this.Type( typeof(T) );

                var result = SyntaxFactory.InvocationExpression( this.GenericSyntaxFactoryMethod( nameof(SyntaxFactory.SingletonSeparatedList), itemType ) )
                    .AddArgumentListArguments( SyntaxFactory.Argument( item ) );

                return result;
            }

            public ExpressionSyntax SeparatedList<T>( IEnumerable<ExpressionSyntax> items )
                where T : SyntaxNode
                => this.List<T>( nameof(SyntaxFactory.SeparatedList), items );

            private ExpressionSyntax List<T>( string methodName, IEnumerable<ExpressionSyntax> items )
                where T : SyntaxNode
            {
                var itemType = this.Type( typeof(T) );

                var argument =
                    SyntaxFactory.ArrayCreationExpression(
                        SyntaxFactory.ArrayType( itemType )
                            .WithRankSpecifiers(
                                SyntaxFactory.SingletonList(
                                    SyntaxFactory.ArrayRankSpecifier(
                                        SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>( SyntaxFactory.OmittedArraySizeExpression() ) ) ) ),
                        SyntaxFactory.InitializerExpression( SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SeparatedList( items ) ) );

                var result = SyntaxFactory.InvocationExpression( this.GenericSyntaxFactoryMethod( methodName, itemType ) )
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList<ArgumentSyntax>( new SyntaxNodeOrToken[] { SyntaxFactory.Argument( argument ) } ) ) );

                return result.NormalizeWhitespace();
            }

            public ExpressionSyntax Identifier( ExpressionSyntax text )
            {
                var result = SyntaxFactory.InvocationExpression( this.SyntaxFactoryMethod( nameof(SyntaxFactory.Identifier) ) )
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList<ArgumentSyntax>( new SyntaxNodeOrToken[] { SyntaxFactory.Argument( text ) } ) ) );

                return result;
            }

            public ExpressionSyntax Identifier(
                ExpressionSyntax leadingTrivia,
                ExpressionSyntax syntaxKind,
                ExpressionSyntax text,
                ExpressionSyntax valueText,
                ExpressionSyntax trailingTrivia )
            {
                var result = SyntaxFactory.InvocationExpression( this.SyntaxFactoryMethod( nameof(SyntaxFactory.Identifier) ) )
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                            [
                                SyntaxFactory.Argument( leadingTrivia ),
                                SyntaxFactory.Argument( syntaxKind ),
                                SyntaxFactory.Argument( text ),
                                SyntaxFactory.Argument( valueText ),
                                SyntaxFactory.Argument( trailingTrivia )
                            ] ) ) );

                return result;
            }

            public ExpressionSyntax Token( ExpressionSyntax kind )
            {
                var result = SyntaxFactory.InvocationExpression( this.SyntaxFactoryMethod( nameof(SyntaxFactory.Token) ) )
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList<ArgumentSyntax>( new SyntaxNodeOrToken[] { SyntaxFactory.Argument( kind ) } ) ) );

                return result;
            }

            public ExpressionSyntax Token(
                ExpressionSyntax leading,
                ExpressionSyntax kind,
                ExpressionSyntax text,
                ExpressionSyntax valueText,
                ExpressionSyntax trailing )
            {
                var result = SyntaxFactory.InvocationExpression( this.SyntaxFactoryMethod( nameof(SyntaxFactory.Token) ) )
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                            [
                                SyntaxFactory.Argument( leading ),
                                SyntaxFactory.Argument( kind ),
                                SyntaxFactory.Argument( text ),
                                SyntaxFactory.Argument( valueText ),
                                SyntaxFactory.Argument( trailing )
                            ] ) ) );

                return result;
            }

            public ExpressionSyntax List<T>( IEnumerable<ExpressionSyntax> items )
                where T : SyntaxNode
                => this.List<T>( nameof(SyntaxFactory.List), items );
        }
    }
}