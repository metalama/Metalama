// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Formatting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Metalama.Open.Virtuosity
{
    [MetalamaPlugIn]
    public class VirtuosityWeaver : IAspectWeaver
    {
        public async Task TransformAsync( AspectWeaverContext context )
        {
            await context.RewriteAspectTargetsAsync( new Rewriter( context ) );
        }

        private class Rewriter : CSharpSyntaxRewriter
        {
            private static readonly SyntaxKind[]? _forbiddenModifiers = new[] { StaticKeyword, VirtualKeyword, OverrideKeyword };
            private static readonly SyntaxKind[]? _requiredModifiers = new[] { PublicKeyword, ProtectedKeyword, InternalKeyword };
            private readonly AspectWeaverContext _context;

            public Rewriter( AspectWeaverContext context )
            {
                this._context = context;
            }

            public override SyntaxNode VisitMethodDeclaration( MethodDeclarationSyntax node )
            {
                if ( node.Parent is not ClassDeclarationSyntax classDeclaration )
                {
                    return node;
                }

                // Note: this won't work if the method is in one part of a partial class and only the other part has the sealed modifier
                if ( classDeclaration.Modifiers.Any( SealedKeyword ) )
                {
                    return node;
                }

                var modifiers = node.Modifiers;

                // Remove the sealed modifier.
                var sealedToken = node.Modifiers.FirstOrDefault( modifier => modifier.IsKind( SyntaxKind.SealedKeyword ) );

                if ( !sealedToken.IsKind( SyntaxKind.None ) )
                {
                    node = node.WithModifiers( node.Modifiers.Remove( sealedToken ) );
                }

                // Add the virtual modifier.
                if ( !_forbiddenModifiers.Any( modifier => node.Modifiers.Any( modifier ) )
                    && _requiredModifiers.Any( modifier => node.Modifiers.Any( modifier ) ) )
                {
                    node = node.AddModifiers(
                        SyntaxFactory.Token( VirtualKeyword )
                                .WithTrailingTrivia( SyntaxFactory.ElasticSpace )
                                .WithGeneratedCodeAnnotation( this._context.GeneratedCodeAnnotation ) );
                }


                return node;
            }
        }
    }
}
