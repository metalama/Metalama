// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Formatting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;

namespace Issue1743
{
    /// <summary>
    /// Makes the public methods of the target type virtual.
    /// </summary>
    /// <remarks>
    /// This file is compiled into both <c>Issue1743.Weaver1</c> and <c>Issue1743.Weaver2</c>, so the compilation of
    /// <c>Issue1743.App</c>, which references both, gets this plug-in type twice under the same full name. See the
    /// README of this directory.
    /// </remarks>
    [MetalamaPlugIn]
    public class DuplicatedWeaver : IAspectWeaver
    {
        /// <inheritdoc />
        public async Task TransformAsync( AspectWeaverContext context )
        {
            await context.RewriteAspectTargetsAsync( new Rewriter( context ) );
        }

        /// <summary>
        /// Adds the <c>virtual</c> modifier to the public instance methods of the types the aspect is applied to.
        /// </summary>
        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly AspectWeaverContext _context;

            /// <summary>
            /// Initializes a new instance of the <see cref="Rewriter"/> class.
            /// </summary>
            public Rewriter( AspectWeaverContext context )
            {
                this._context = context;
            }

            /// <inheritdoc />
            public override SyntaxNode VisitMethodDeclaration( MethodDeclarationSyntax node )
            {
                if ( node.Parent is not ClassDeclarationSyntax classDeclaration
                     || classDeclaration.Modifiers.Any( SyntaxKind.SealedKeyword ) )
                {
                    return node;
                }

                if ( !node.Modifiers.Any( SyntaxKind.PublicKeyword )
                     || node.Modifiers.Any( SyntaxKind.StaticKeyword )
                     || node.Modifiers.Any( SyntaxKind.VirtualKeyword )
                     || node.Modifiers.Any( SyntaxKind.OverrideKeyword ) )
                {
                    return node;
                }

                return node.AddModifiers(
                    SyntaxFactory.Token( SyntaxKind.VirtualKeyword )
                        .WithTrailingTrivia( SyntaxFactory.ElasticSpace )
                        .WithGeneratedCodeAnnotation( this._context.GeneratedCodeAnnotation ) );
            }
        }
    }
}
