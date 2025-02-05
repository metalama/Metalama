// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating
{
    internal sealed partial class TemplateCompilerRewriter
    {
        private const string _templateSyntaxFactoryParameterName = "templateSyntaxFactory";
        private const string _templateSyntaxFactoryLocalName = "localTemplateSyntaxFactory";

        /// <summary>
        /// Generates code that invokes members of the <see cref="ITemplateSyntaxFactory"/> class.
        /// </summary>
        private sealed class TemplateMetaSyntaxFactoryImpl
        {
            private readonly IdentifierNameSyntax _templateSyntaxFactoryIdentifier;

            public TemplateMetaSyntaxFactoryImpl( string templateSyntaxFactoryIdentifier )
            {
                this._templateSyntaxFactoryIdentifier = SyntaxFactory.IdentifierName( templateSyntaxFactoryIdentifier );
            }

            /// <summary>
            /// Generates a <see cref="MemberAccessExpressionSyntax"/> that represents the fully-qualified name
            /// of a method of the <see cref="ITemplateSyntaxFactory"/> class.
            /// </summary>
            /// <param name="name">Name of the method.</param>
            /// <returns></returns>
            public MemberAccessExpressionSyntax TemplateSyntaxFactoryMember( string name )
                => SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    this._templateSyntaxFactoryIdentifier,
                    SyntaxFactory.IdentifierName( name ) );

            public MemberAccessExpressionSyntax GenericTemplateSyntaxFactoryMember( string name, params TypeSyntax[] genericParameters )
                => SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    this._templateSyntaxFactoryIdentifier,
                    SyntaxFactory.GenericName( SyntaxFactory.Identifier( name ) )
                        .WithTypeArgumentList( SyntaxFactory.TypeArgumentList( SyntaxFactory.SeparatedList( genericParameters ) ) ) );

            /// <summary>
            /// Generates a call to the <see cref="ITemplateSyntaxFactory.GetUniqueIdentifier"/> method.
            /// </summary>
            public ExpressionSyntax GetUniqueIdentifier( string hint )
                => SyntaxFactory.InvocationExpression( this.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.GetUniqueIdentifier) ) )
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                new SyntaxNodeOrToken[] { SyntaxFactory.Argument( SyntaxFactoryEx.LiteralExpression( hint ) ) } ) ) );
        }
    }
}