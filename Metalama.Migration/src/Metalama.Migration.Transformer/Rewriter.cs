// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Migration.Transformer;

internal class Rewriter : CSharpSyntaxRewriter
{
    private static readonly Regex _cleanTextRegex = new( "[\\s]+" );
    private readonly SemanticModel _semanticModel;
    private readonly HashSet<ISymbol> _processedPartialTypes;

    public Rewriter( SemanticModel semanticModel, HashSet<ISymbol> processedPartialTypes )
    {
        this._semanticModel = semanticModel;
        this._processedPartialTypes = processedPartialTypes;
    }

    private T RewriteAttributes<T>(
        T node,
        Func<T, SyntaxNode?> callBase,
        Func<T, SyntaxList<AttributeListSyntax>> getAttributes,
        Func<T, SyntaxList<AttributeListSyntax>, T> changeAttributes,
        Func<T, SyntaxNode>? getSymbolNode = null )
        where T : SyntaxNode
    {
        var transformedNode = (T) callBase( node )!;

        var symbolNode = getSymbolNode?.Invoke( node ) ?? node;

        var symbol = this._semanticModel.GetDeclaredSymbol( symbolNode );

        if ( symbol == null )
        {
            return transformedNode;
        }

        var attributes = getAttributes( transformedNode );

        var obsoleteAttribute = attributes.SelectMany( a => a.Attributes ).SingleOrDefault( a => a.Name.ToString() == "Obsolete" );

        var isError = obsoleteAttribute is { ArgumentList.Arguments.Count: > 1 }
                      && obsoleteAttribute.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax literal &&
                      literal.Token.IsKind( SyntaxKind.TrueKeyword );

        var xml = symbol.GetDocumentationCommentXml( CultureInfo.InvariantCulture, true );

        string? obsoleteMessage = null;

        if ( !string.IsNullOrEmpty( xml ) )
        {
            var xmlDocument = XDocument.Parse( xml );
            var summary = xmlDocument.Root?.Element( "summary" );

            if ( summary != null )
            {
                var stringBuilder = new StringBuilder();

                foreach ( var item in summary.Nodes() )
                {
                    switch ( item )
                    {
                        case XText text:
                            var content = _cleanTextRegex.Replace( text.Value, " " );
                            stringBuilder.Append( content );

                            break;

                        case XElement element when element.Attribute( "cref" ) is { } cref:
                            var referencedSymbol = DocumentationCommentId.GetFirstSymbolForDeclarationId( cref.Value, this._semanticModel.Compilation );

                            stringBuilder.Append( "'" );

                            if ( referencedSymbol != null )
                            {
                                stringBuilder.Append( referencedSymbol.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat ) );
                            }
                            else
                            {
                                stringBuilder.Append( cref.Value );
                            }

                            stringBuilder.Append( "'" );

                            break;

                        case XElement element when element.Attribute( "name" ) is { } name:
                            stringBuilder.Append( "'" );
                            stringBuilder.Append( name.Value );
                            stringBuilder.Append( "'" );

                            break;

                        default:
                            stringBuilder.Append( item );

                            break;
                    }
                }

                obsoleteMessage = stringBuilder.ToString().Trim();
            }
        }

        if ( obsoleteMessage == null && !isError )
        {
            return transformedNode;
        }

        obsoleteMessage ??= "This API call must be ported from PostSharp to Metalama.";

        var newObsoleteAttribute =
            Attribute(
                QualifiedName( IdentifierName( "System" ), IdentifierName( "Obsolete" ) ),
                AttributeArgumentList(
                    SeparatedList(
                        new[]
                        {
                            AttributeArgument( LiteralExpression( SyntaxKind.StringLiteralExpression, Literal( obsoleteMessage ) ) ),
                            AttributeArgument( LiteralExpression( isError ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression ) )
                        } ) ) );

        SyntaxList<AttributeListSyntax> newAttributes;

        if ( obsoleteAttribute != null )
        {
            var oldAttributeList = attributes.Single( l => l.Attributes.Contains( obsoleteAttribute ) );
            newAttributes = attributes.Replace( oldAttributeList, oldAttributeList.ReplaceNode( obsoleteAttribute, newObsoleteAttribute ) );

            return changeAttributes( transformedNode, newAttributes );
        }
        else if ( attributes.Count > 0 )
        {
            newAttributes = attributes.Add( AttributeList( SingletonSeparatedList( newObsoleteAttribute ) ).WithTrailingTrivia( LineFeed ) );

            return changeAttributes( transformedNode, newAttributes );
        }
        else
        {
            // We need to move the trivia from the declaration to the attribute list because of XML comments.

            newAttributes = attributes.Add(
                AttributeList( SingletonSeparatedList( newObsoleteAttribute ) ).WithLeadingTrivia( node.GetLeadingTrivia() ).WithTrailingTrivia( LineFeed ) );

            return changeAttributes( transformedNode.WithLeadingTrivia( SyntaxTriviaList.Empty ), newAttributes );
        }
    }

    public override SyntaxNode? VisitClassDeclaration( ClassDeclarationSyntax node )
    {
        // Avoid duplicate attributes on partial classes.
        if ( node.Modifiers.Any( t => t.IsKind( SyntaxKind.PartialKeyword ) ) )
        {
            var symbol = this._semanticModel.GetDeclaredSymbol( node );

            if ( symbol == null || !this._processedPartialTypes.Add( symbol ) )
            {
                return base.VisitClassDeclaration( node );
            }
        }

        return this.RewriteAttributes( node, base.VisitClassDeclaration, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
    }

    public override SyntaxNode? VisitPropertyDeclaration( PropertyDeclarationSyntax node )
        => this.RewriteAttributes( node, base.VisitPropertyDeclaration, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

    public override SyntaxNode? VisitIndexerDeclaration( IndexerDeclarationSyntax node )
        => this.RewriteAttributes( node, base.VisitIndexerDeclaration, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

    public override SyntaxNode? VisitStructDeclaration( StructDeclarationSyntax node )
        => this.RewriteAttributes( node, base.VisitStructDeclaration, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

    public override SyntaxNode? VisitDelegateDeclaration( DelegateDeclarationSyntax node )
        => this.RewriteAttributes( node, base.VisitDelegateDeclaration, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

    public override SyntaxNode? VisitFieldDeclaration( FieldDeclarationSyntax node )
        => this.RewriteAttributes(
            node,
            base.VisitFieldDeclaration,
            n => n.AttributeLists,
            ( n, a ) => n.WithAttributeLists( a ),
            f => f.Declaration.Variables[0] );

    public override SyntaxNode? VisitEventFieldDeclaration( EventFieldDeclarationSyntax node )
        => this.RewriteAttributes(
            node,
            base.VisitEventFieldDeclaration,
            n => n.AttributeLists,
            ( n, a ) => n.WithAttributeLists( a ),
            f => f.Declaration.Variables[0] );

    public override SyntaxNode? VisitEnumDeclaration( EnumDeclarationSyntax node )
        => this.RewriteAttributes( node, base.VisitEnumDeclaration, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

    public override SyntaxNode? VisitInterfaceDeclaration( InterfaceDeclarationSyntax node )
        => this.RewriteAttributes( node, base.VisitInterfaceDeclaration, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

    public override SyntaxNode? VisitConstructorDeclaration( ConstructorDeclarationSyntax node )
        => this.RewriteAttributes( node, base.VisitConstructorDeclaration, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

    public override SyntaxNode? VisitEnumMemberDeclaration( EnumMemberDeclarationSyntax node )
        => this.RewriteAttributes( node, base.VisitEnumMemberDeclaration, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
}