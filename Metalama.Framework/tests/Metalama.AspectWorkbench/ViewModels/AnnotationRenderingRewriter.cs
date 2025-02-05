// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Linking;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.AspectWorkbench.ViewModels
{
    internal sealed class AnnotationRenderingRewriter : CSharpSyntaxRewriter
    {
        [return: NotNullIfNotNull( "node" )]
        public override SyntaxNode? Visit( SyntaxNode? node )
        {
            var transformedNode = base.Visit( node );

            if ( node != null && transformedNode != null )
            {
                var aspectReferenceAnnotation = node.GetAnnotations( AspectReferenceAnnotationExtensions.AnnotationKind ).FirstOrDefault();

                if ( aspectReferenceAnnotation != null )
                {
                    // ReSharper disable once StringLiteralTypo
                    transformedNode = transformedNode
                        .WithLeadingTrivia(
                            transformedNode.GetLeadingTrivia()
                                .Add(
                                    SyntaxTrivia(
                                        SyntaxKind.MultiLineCommentTrivia,
                                        $"/*REF({aspectReferenceAnnotation.Data})*/" ) ) )
                        .WithTrailingTrivia(
                            transformedNode.GetTrailingTrivia()
                                .Insert(
                                    0,
                                    SyntaxTrivia(
                                        SyntaxKind.MultiLineCommentTrivia,
                                        "/*ENDREF*/" ) ) );
                }

                var declarationAnnotation = node.GetAnnotations( AspectLinkerDeclarationAnnotationExtensions.DeclarationAnnotationKind ).FirstOrDefault();

                if ( declarationAnnotation != null )
                {
                    transformedNode = transformedNode
                        .WithLeadingTrivia(
                            transformedNode.GetLeadingTrivia()
                                .Add(
                                    SyntaxTrivia(
                                        SyntaxKind.MultiLineCommentTrivia,
                                        $"/*FLAGS({declarationAnnotation.Data})*/" ) )
                                .Add( ElasticLineFeed ) );
                }

                var generatedCodeAnnotation = node.GetAnnotations( MetalamaCompilerAnnotations.GeneratedCodeAnnotationKind ).FirstOrDefault();

                if ( generatedCodeAnnotation != null )
                {
                    // ReSharper disable once StringLiteralTypo
                    transformedNode = transformedNode
                        .WithLeadingTrivia(
                            transformedNode.GetLeadingTrivia()
                                .Add(
                                    SyntaxTrivia(
                                        SyntaxKind.MultiLineCommentTrivia,
                                        $"/*+G*/" ) ) )
                        .WithTrailingTrivia(
                            transformedNode.GetTrailingTrivia()
                                .Insert(
                                    0,
                                    SyntaxTrivia(
                                        SyntaxKind.MultiLineCommentTrivia,
                                        "/*-G*/" ) ) );
                }

                var sourceCodeAnnotation = node.GetAnnotations( MetalamaCompilerAnnotations.SourceCodeAnnotation.Kind ).FirstOrDefault();

                if ( sourceCodeAnnotation != null )
                {
                    // ReSharper disable once StringLiteralTypo
                    transformedNode = transformedNode
                        .WithLeadingTrivia(
                            transformedNode.GetLeadingTrivia()
                                .Add(
                                    SyntaxTrivia(
                                        SyntaxKind.MultiLineCommentTrivia,
                                        $"/*+S*/" ) ) )
                        .WithTrailingTrivia(
                            transformedNode.GetTrailingTrivia()
                                .Insert(
                                    0,
                                    SyntaxTrivia(
                                        SyntaxKind.MultiLineCommentTrivia,
                                        "/*-S*/" ) ) );
                }

                return transformedNode;
            }
            else
            {
                return transformedNode;
            }
        }
    }
}