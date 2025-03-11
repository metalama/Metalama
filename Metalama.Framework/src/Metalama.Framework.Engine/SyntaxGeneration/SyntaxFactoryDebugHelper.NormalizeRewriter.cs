// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Engine.SyntaxGeneration;

public static partial class SyntaxFactoryDebugHelper
{
    private sealed class NormalizeRewriter : SafeSyntaxRewriter
    {
        public NormalizeRewriter() : base( true ) { }

        public override SyntaxNode? VisitQualifiedName( QualifiedNameSyntax node )
        {
            if ( node.Parent == null )
            {
                throw new AssertionFailedException( "The parent node is null." );
            }

            // The following list of exceptions is incomplete. If you get into an InvalidCastException in the rewriter, you have to extend it.
            if ( !node.AncestorsAndSelf()
                    .Any(
                        a =>
                            a is GenericNameSyntax or UsingDirectiveSyntax ||
                            (a.Parent is NamespaceDeclarationSyntax namespaceDeclaration && namespaceDeclaration.Name == a) ||
                            (a.Parent is FileScopedNamespaceDeclarationSyntax fileScopeNamespaceDeclaration && fileScopeNamespaceDeclaration.Name == a) ||
                            (a.Parent is MethodDeclarationSyntax methodDeclaration && methodDeclaration.ReturnType == a) ||
                            (a.Parent is VariableDeclarationSyntax variable && variable.Type == a) ||
                            (a.Parent is TypeConstraintSyntax typeConstraint && typeConstraint.Type == a) ||
                            (a.Parent is ArrayTypeSyntax arrayType && arrayType.ElementType == a) ||
                            (a.Parent is ObjectCreationExpressionSyntax objectCreation && objectCreation.Type == a) ||
                            (a.Parent is DefaultExpressionSyntax defaultExpression && defaultExpression.Type == a) ||
                            (a.Parent is CastExpressionSyntax castExpression && castExpression.Type == a) ||
                            (a.Parent is ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier && explicitInterfaceSpecifier.Name == a) ||
                            (a.Parent is ParameterSyntax parameter && parameter.Type == a) ||
                            (a.Parent is PropertyDeclarationSyntax property && property.Type == a) ||
                            (a.Parent is EventDeclarationSyntax @event && @event.Type == a) ||
                            a.Parent is SimpleBaseTypeSyntax or TypeOfExpressionSyntax ) )
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    (ExpressionSyntax) this.Visit( node.Left )!,
                    node.DotToken,
                    node.Right );
            }
            else
            {
                return base.VisitQualifiedName( node );
            }
        }

        public override SyntaxNode? VisitXmlComment( XmlCommentSyntax node ) => null;

        public override SyntaxNode? VisitDocumentationCommentTrivia( DocumentationCommentTriviaSyntax node ) => null;

        public override SyntaxTrivia VisitTrivia( SyntaxTrivia trivia )
        {
            switch ( trivia.Kind() )
            {
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                    return default;

                default:
                    return trivia;
            }
        }

        public override SyntaxNode? VisitPragmaWarningDirectiveTrivia( PragmaWarningDirectiveTriviaSyntax node ) => null;
    }
}