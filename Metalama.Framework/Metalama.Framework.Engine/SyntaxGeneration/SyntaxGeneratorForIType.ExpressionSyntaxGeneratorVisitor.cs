// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.SyntaxGeneration;

internal sealed partial class SyntaxGeneratorForIType
{
    private sealed class ExpressionSyntaxGeneratorVisitor : AbstractGeneratorVisitor<ExpressionSyntax>
    {
        public ExpressionSyntaxGeneratorVisitor( SyntaxGeneratorForIType syntaxGeneratorForIType ) : base( syntaxGeneratorForIType ) { }

        protected override ExpressionSyntax VisitNamedType( INamedType namedType )
        {
            var typeSyntax = this.SyntaxGenerator._typeSyntaxGeneratorVisitor.CreateSimpleTypeSyntax( namedType );

            if ( typeSyntax is not SimpleNameSyntax simpleNameSyntax )
            {
                return typeSyntax;
            }

            if ( namedType.DeclaringType != null )
            {
                var container = this.VisitNamedType( namedType.DeclaringType );

                return this.CreateMemberAccessExpression( namedType, container, simpleNameSyntax );
            }
            else
            {
                if ( namedType.ContainingNamespace.IsGlobalNamespace )
                {
                    if ( namedType.TypeKind != TypeKind.Error )
                    {
                        return this.AddInformationTo(
                            SyntaxFactory.AliasQualifiedName(
                                SyntaxFactory.IdentifierName( SyntaxFactory.Token( SyntaxKind.GlobalKeyword ) ),
                                simpleNameSyntax ),
                            namedType );
                    }
                }
                else
                {
                    var container = this.VisitNamespace( namedType.ContainingNamespace );

                    return this.CreateMemberAccessExpression( namedType, container, simpleNameSyntax );
                }
            }

            return simpleNameSyntax;
        }

        private ExpressionSyntax VisitNamespace( INamespace ns )
        {
            var syntax = this.AddInformationTo( ToIdentifierName( ns.Name ), ns );

            if ( ns.ContainingNamespace == null )
            {
                return syntax;
            }

            if ( ns.ContainingNamespace.IsGlobalNamespace )
            {
                return this.AddInformationTo(
                    SyntaxFactory.AliasQualifiedName(
                        SyntaxFactory.IdentifierName( SyntaxFactory.Token( SyntaxKind.GlobalKeyword ) ),
                        syntax ),
                    ns );
            }
            else
            {
                var container = this.VisitNamespace( ns.ContainingNamespace );

                return this.CreateMemberAccessExpression( ns, container, syntax );
            }
        }

        private MemberAccessExpressionSyntax CreateMemberAccessExpression( INamespaceOrNamedType symbol, ExpressionSyntax container, SimpleNameSyntax syntax )
        {
            return this.AddInformationTo(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    container,
                    syntax ),
                symbol );
        }
    }
}