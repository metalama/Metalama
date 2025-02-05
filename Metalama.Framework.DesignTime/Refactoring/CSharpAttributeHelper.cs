// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Metalama.Framework.DesignTime.Refactoring
{
    public static class CSharpAttributeHelper
    {
        public static async ValueTask<SyntaxNode?> AddAttributeAsync(
            SyntaxNode oldRoot,
            SyntaxNode? oldNode,
            AttributeDescription attribute,
            SyntaxGenerationContext context,
            CancellationToken cancellationToken )
        {
            // target syntax node doesn't exist anymore, nothing to be done here
            if ( oldNode == null )
            {
                return oldRoot;
            }

            if ( oldNode.IsKind( SyntaxKind.VariableDeclarator ) && oldNode.Parent is { Parent: FieldDeclarationSyntax fieldDeclarationSyntax } )
            {
                oldNode = fieldDeclarationSyntax;
            }

            var newNode = AddAttribute( oldNode, attribute, context );

            if ( newNode == null )
            {
                return null;
            }

            var newRoot = oldRoot.ReplaceNode( oldNode, newNode );

            foreach ( var ns in attribute.Imports )
            {
                if ( string.IsNullOrEmpty( ns ) )
                {
                    continue;
                }

                if ( await newRoot.SyntaxTree.GetRootAsync( cancellationToken ) is CompilationUnitSyntax newUnit )
                {
                    if ( newUnit.Usings.All( u => u.Name?.ToString() != ns ) )
                    {
                        newRoot =
                            newUnit.AddUsings(
                                SyntaxFactory.UsingDirective( SyntaxFactory.IdentifierName( ns ).WithLeadingTrivia( SyntaxFactory.ElasticSpace ) )
                                    .WithTrailingTrivia( context.ElasticEndOfLineTriviaList )
                                    .WithAdditionalAnnotations( Formatter.Annotation ) );
                    }
                }
            }

            return newRoot;
        }

        private static SyntaxNode? AddAttribute( SyntaxNode oldNode, AttributeDescription attribute, SyntaxGenerationContext context )
        {
            var newNode = oldNode.WithoutLeadingTrivia();

            var attributeList = CreateAttributeSyntax( attribute, forAssembly: oldNode.IsKind( SyntaxKind.CompilationUnit ) )
                .WithAdditionalAnnotations( Formatter.Annotation );

            switch ( oldNode.Kind() )
            {
                case SyntaxKind.MethodDeclaration:
                    newNode = ((MethodDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.DestructorDeclaration:
                    newNode = ((DestructorDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.ConstructorDeclaration:
                    newNode = ((ConstructorDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.InterfaceDeclaration:
                    newNode = ((InterfaceDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.DelegateDeclaration:
                    newNode = ((DelegateDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.EnumDeclaration:
                    newNode = ((EnumDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.ClassDeclaration:
                    newNode = ((ClassDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.StructDeclaration:
                    newNode = ((StructDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.Parameter:
                    newNode = ((ParameterSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.PropertyDeclaration:
                    newNode = ((PropertyDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.EventDeclaration:
                    newNode = ((EventDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                    newNode = ((AccessorDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.OperatorDeclaration:
                    newNode = ((OperatorDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.ConversionOperatorDeclaration:
                    newNode = ((ConversionOperatorDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.IndexerDeclaration:
                    newNode = ((IndexerDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.FieldDeclaration:
                    newNode = ((FieldDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.EventFieldDeclaration:
                    newNode = ((EventFieldDeclarationSyntax) newNode).AddAttributeLists( attributeList );

                    break;

                case SyntaxKind.CompilationUnit:
                    // We use oldNode here, because we need to handle trivia differently.
                    var compilationUnit = (CompilationUnitSyntax) oldNode;

                    if ( !compilationUnit.Members.Any() )
                    {
                        SyntaxTriviaList trivia = default;

                        if ( compilationUnit.EndOfFileToken.HasLeadingTrivia )
                        {
                            trivia = compilationUnit.EndOfFileToken.LeadingTrivia;
                            compilationUnit = compilationUnit.WithEndOfFileToken( compilationUnit.EndOfFileToken.WithLeadingTrivia() );
                        }

                        // Add a new line if the file is not empty and we don't already have one.
                        if ( oldNode.FullSpan.Length != 0 && !trivia.LastOrDefault().IsKind( SyntaxKind.EndOfLineTrivia ) )
                        {
                            trivia = trivia.AddRange( context.ElasticEndOfLineTriviaList );
                        }

                        attributeList = attributeList.WithLeadingTrivia( trivia );
                    }

                    return compilationUnit.AddAttributeLists( attributeList );

                default:
                    return null;
            }

            return newNode.WithLeadingTrivia( oldNode.GetLeadingTrivia() );
        }

        internal static async ValueTask<Solution> AddAttributeAsync(
            Document document,
            ISymbol symbol,
            AttributeDescription attribute,
            SyntaxGenerationContext context,
            CancellationToken cancellationToken )
        {
            var currentSolution = document.Project.Solution;
            var oldRoot = (CompilationUnitSyntax?) await document.GetSyntaxRootAsync( cancellationToken );

            if ( oldRoot == null )
            {
                // Error.
                return document.Project.Solution;
            }

            var oldNode = await symbol.DeclaringSyntaxReferences.Single( r => r.SyntaxTree == oldRoot.SyntaxTree ).GetSyntaxAsync( cancellationToken );

            var newRoot = await AddAttributeAsync( oldRoot, oldNode, attribute, context, cancellationToken );

            if ( newRoot == null )
            {
                // Error.
                return document.Project.Solution;
            }

            newRoot = Formatter.Format( newRoot, Formatter.Annotation, currentSolution.Workspace, cancellationToken: cancellationToken );

            var newSolution = currentSolution.WithDocumentSyntaxRoot( document.Id, newRoot );

            return newSolution;
        }

        internal static async ValueTask<Solution> AddAttributeAsync(
            Document document,
            SyntaxNode node,
            AttributeDescription attribute,
            SyntaxGenerationContext context,
            CancellationToken cancellationToken )
        {
            var currentSolution = document.Project.Solution;

            var oldNode = node;
            var oldRoot = oldNode.SyntaxTree.GetCompilationUnitRoot( cancellationToken );

            var newRoot = await AddAttributeAsync( oldRoot, oldNode, attribute, context, cancellationToken );

            if ( newRoot == null )
            {
                // Error.
                return document.Project.Solution;
            }

            newRoot = Formatter.Format( newRoot, Formatter.Annotation, currentSolution.Workspace, cancellationToken: cancellationToken );

            var newSolution = currentSolution.WithDocumentSyntaxRoot( document.Id, newRoot );

            return newSolution;
        }

        private static AttributeListSyntax CreateAttributeSyntax( AttributeDescription attribute, bool forAssembly = false )
        {
            var target = forAssembly ? SyntaxFactory.AttributeTargetSpecifier( SyntaxFactory.Identifier( "assembly" ) ) : null;

            AttributeArgumentListSyntax? argumentList = null;

            if ( attribute.Arguments.Any() || attribute.Properties.Any() )
            {
                var arguments = attribute.Arguments.Select( a => SyntaxFactory.AttributeArgument( SyntaxFactory.ParseExpression( a ) ) );

                var properties = attribute.Properties.Select(
                    property => SyntaxFactory.AttributeArgument(
                        SyntaxFactory.NameEquals( property.Name ),
                        nameColon: null,
                        SyntaxFactory.ParseExpression( property.Value ) ) );

                argumentList = SyntaxFactory.AttributeArgumentList( SyntaxFactory.SeparatedList( arguments.Concat( properties ) ) );
            }

            return SyntaxFactory.AttributeList(
                target,
                SyntaxFactory.SingletonSeparatedList( SyntaxFactory.Attribute( SyntaxFactory.ParseName( attribute.Name ), argumentList ) ) );
        }
    }
}