// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking.Substitution;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking
{
    internal sealed partial class LinkerRewritingDriver
    {
        // Destructors/finalizers are only override targets, overrides are always represented as methods.

        private IReadOnlyList<MemberDeclarationSyntax> RewriteDestructor(
            DestructorDeclarationSyntax destructorDeclaration,
            IMethodSymbol symbol,
            SyntaxGenerationContext generationContext )
        {
            if ( this.InjectionRegistry.IsOverrideTarget( symbol ) )
            {
                var members = new List<MemberDeclarationSyntax>();
                var lastOverride = this.InjectionRegistry.GetLastOverride( symbol );

                if ( this.AnalysisRegistry.IsInlined( lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
                {
                    members.Add( GetLinkedDeclaration( IntermediateSymbolSemanticKind.Final ) );
                }
                else
                {
                    members.Add( this.GetTrampolineDestructor( destructorDeclaration, lastOverride, generationContext ) );
                }

                if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                     && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                     && this.ShouldGenerateSourceMember( symbol ) )
                {
                    members.Add( this.GetOriginalImplDestructor( destructorDeclaration, symbol, generationContext ) );
                }

                if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && this.ShouldGenerateEmptyMember( symbol ) )
                {
                    members.Add( this.GetEmptyImplDestructor( destructorDeclaration, symbol, generationContext ) );
                }

                return members;
            }
            else if ( this.AnalysisRegistry.HasAnySubstitutions( symbol ) )
            {
                return [GetLinkedDeclaration( IntermediateSymbolSemanticKind.Default )];
            }
            else
            {
                return [destructorDeclaration];
            }

            DestructorDeclarationSyntax GetLinkedDeclaration( IntermediateSymbolSemanticKind semanticKind )
            {
                var linkedBody = this.GetSubstitutedBody(
                    symbol.ToSemantic( semanticKind ),
                    new SubstitutionContext(
                        this,
                        generationContext,
                        new InliningContextIdentifier( symbol.ToSemantic( semanticKind ) ) ) );

                // Trivia processing:
                //   * For block bodies methods, we preserve trivia of the opening/closing brace.
                //   * For expression bodied methods:
                //       int Foo() <trivia_leading_equals_value> => <trivia_trailing_equals_value> <expression> <trivia_leading_semicolon> ; <trivia_trailing_semicolon>
                //       int Foo() <trivia_leading_equals_value> { <trivia_trailing_equals_value> <linked_body> <trivia_leading_semicolon> } <trivia_trailing_semicolon>

                var (openBraceLeadingTrivia, openBraceTrailingTrivia, closeBraceLeadingTrivia, closeBraceTrailingTrivia) =
                    destructorDeclaration switch
                    {
                        { Body: { OpenBraceToken: var openBraceToken, CloseBraceToken: var closeBraceToken } } =>
                            (openBraceToken.LeadingTrivia, openBraceToken.TrailingTrivia, closeBraceToken.LeadingTrivia, closeBraceToken.TrailingTrivia),
                        { ExpressionBody.ArrowToken: var arrowToken, SemicolonToken: var semicolonToken } =>
                            (arrowToken.LeadingTrivia.AddOptionalLineFeed( generationContext ),
                             arrowToken.TrailingTrivia.AddOptionalLineFeed( generationContext ),
                             semicolonToken.LeadingTrivia.AddOptionalLineFeed( generationContext ), semicolonToken.TrailingTrivia),
                        _ => throw new AssertionFailedException( $"Unexpected destructor declaration {destructorDeclaration}" )
                    };

                var ret = destructorDeclaration.PartialUpdate(
                    expressionBody: null,
                    modifiers: destructorDeclaration.Modifiers,
                    body: Block(
                            Token( openBraceLeadingTrivia, SyntaxKind.OpenBraceToken, openBraceTrailingTrivia ),
                            SingletonList<StatementSyntax>( linkedBody ),
                            Token( closeBraceLeadingTrivia, SyntaxKind.CloseBraceToken, closeBraceTrailingTrivia ) )
                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock )
                        .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ),
                    semicolonToken: default(SyntaxToken) );

                return ret;
            }
        }

        private MemberDeclarationSyntax GetOriginalImplDestructor(
            DestructorDeclarationSyntax destructor,
            IMethodSymbol symbol,
            SyntaxGenerationContext generationContext )
        {
            var semantic = symbol.ToSemantic( IntermediateSymbolSemanticKind.Default );
            var context = new InliningContextIdentifier( semantic );

            var substitutedBody =
                destructor.Body != null
                    ? (BlockSyntax) RewriteBody( destructor.Body, symbol, new SubstitutionContext( this, generationContext, context ) )
                    : null;

            var substitutedExpressionBody =
                destructor.ExpressionBody != null
                    ? (ArrowExpressionClauseSyntax) RewriteBody(
                        destructor.ExpressionBody,
                        symbol,
                        new SubstitutionContext( this, generationContext, context ) )
                    : null;

            return this.GetSpecialImplDestructor(
                destructor,
                substitutedBody.WithSourceCodeAnnotation(),
                substitutedExpressionBody.WithSourceCodeAnnotation(),
                symbol,
                GetOriginalImplMemberName( symbol ),
                generationContext );
        }

        private MemberDeclarationSyntax GetEmptyImplDestructor(
            DestructorDeclarationSyntax destructor,
            IMethodSymbol symbol,
            SyntaxGenerationContext context )
        {
            var emptyBody = context.SyntaxGenerator.FormattedBlock();

            return this.GetSpecialImplDestructor( destructor, emptyBody, null, symbol, GetEmptyImplMemberName( symbol ), context );
        }

        private MemberDeclarationSyntax GetSpecialImplDestructor(
            DestructorDeclarationSyntax destructor,
            BlockSyntax? body,
            ArrowExpressionClauseSyntax? expressionBody,
            IMethodSymbol symbol,
            string name,
            SyntaxGenerationContext context )
        {
            var modifiers = symbol
                .GetSyntaxModifierList( ModifierCategories.Static | ModifierCategories.Unsafe | ModifierCategories.Async )
                .Insert( 0, SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) );

            return
                MethodDeclaration(
                        this.FilterAttributesOnSpecialImpl( symbol ),
                        modifiers,
                        PredefinedType( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.VoidKeyword ) ),
                        null,
                        Identifier( name ),
                        null,
                        destructor.ParameterList.WithOptionalTrailingTrivia( default(SyntaxTriviaList), this.SyntaxGenerationOptions ),
                        List<TypeParameterConstraintClauseSyntax>(),
                        body,
                        expressionBody )
                    .WithOptionalLeadingAndTrailingLineFeed( context )
                    .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
        }

        private DestructorDeclarationSyntax GetTrampolineDestructor(
            DestructorDeclarationSyntax destructor,
            IMethodSymbol targetSymbol,
            SyntaxGenerationContext context )
        {
            // TODO: First override not being inlineable probably does not happen outside of specifically written linker tests, i.e. trampolines may not be needed.

            return
                destructor
                    .WithBody( GetBody() )
                    .WithTriviaFromIfNecessary( destructor, this.SyntaxGenerationOptions );

            BlockSyntax GetBody()
            {
                var invocation =
                    InvocationExpression(
                        MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName( targetSymbol.Name ) )
                            .WithSimplifierAnnotationIfNecessary( context ),
                        ArgumentList() );

                return context.SyntaxGenerator.FormattedBlock(
                    ReturnStatement(
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                        invocation,
                        Token( SyntaxKind.SemicolonToken ) ) );
            }
        }
    }
}