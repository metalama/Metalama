// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
#pragma warning disable LAMA0850 // Namespace from user code
                        newRoot =
                            newUnit.AddUsings(
                                SyntaxFactory.UsingDirective( SyntaxFactory.IdentifierName( ns ).WithLeadingTrivia( SyntaxFactory.ElasticSpace ) )
                                    .WithTrailingTrivia( context.OptionalElasticEndOfLineTriviaList )
                                    .WithAdditionalAnnotations( Formatter.Annotation ) );
#pragma warning restore LAMA0850
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

            switch ( newNode )
            {
                case ParameterSyntax parameter:
                    newNode = parameter.AddAttributeLists( attributeList );

                    break;

                case AccessorDeclarationSyntax accessor:
                    newNode = accessor.AddAttributeLists( attributeList );

                    break;

                case CompilationUnitSyntax:
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
                            trivia = trivia.AddRange( context.OptionalElasticEndOfLineTriviaList );
                        }

                        attributeList = attributeList.WithLeadingTrivia( trivia );
                    }

                    return compilationUnit.AddAttributeLists( attributeList );

                // These nodes derive from MemberDeclarationSyntax, but an attribute list on them would not be valid
                // code, so they are excluded before the general arm below.
                case BaseNamespaceDeclarationSyntax:
                case EnumMemberDeclarationSyntax:
                case GlobalStatementSyntax:
                case IncompleteMemberSyntax:
                    return null;

                // Every type declaration and every other member declaration, which includes the record, the record
                // struct and the extension block. A declaration kind added by a later version of the language is
                // covered without a new arm.
                case MemberDeclarationSyntax member:
                    newNode = member.AddAttributeLists( attributeList );

                    break;

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
#pragma warning disable LAMA0850 // "assembly" is well-known
            var target = forAssembly ? SyntaxFactory.AttributeTargetSpecifier( SyntaxFactory.Identifier( "assembly" ) ) : null;
#pragma warning restore LAMA0850

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