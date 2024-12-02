// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceMethodTransformation : IntroduceMemberTransformation<MethodBuilderData>
{
    public IntroduceMethodTransformation( AspectLayerInstance aspectLayerInstance, MethodBuilderData introducedDeclaration )
        : base( aspectLayerInstance, introducedDeclaration ) { }

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var finalMethod = this.BuilderData.ToRef().GetTarget( context.FinalCompilation );

        var syntaxGenerator = context.SyntaxGenerationContext.SyntaxGenerator;

        var explicitInterfaceSpecifier = finalMethod.ExplicitInterfaceImplementations.Count > 0
            ? ExplicitInterfaceSpecifier( (NameSyntax) syntaxGenerator.TypeSyntax( finalMethod.ExplicitInterfaceImplementations.Single().DeclaringType ) )
            : null;

        var hasNoBody = finalMethod.IsAbstract || finalMethod.IsPartial || finalMethod.IsExtern;

        switch ( finalMethod.DeclarationKind )
        {
            case DeclarationKind.Finalizer:
                {
                    Invariant.Assert( !hasNoBody );

                    var syntax = DestructorDeclaration(
                        AdviceSyntaxGenerator.GetAttributeLists( finalMethod, context ),
                        TokenList(),
                        ((TypeDeclarationSyntax) finalMethod.DeclaringType.GetPrimaryDeclarationSyntax().AssertNotNull()).Identifier,
                        ParameterList(),
                        Block().WithGeneratedCodeAnnotation( this.AspectInstance.AspectClass.GeneratedCodeAnnotation ),
                        null );

                    return [new InjectedMember( this, syntax, this.AspectLayerId, InjectedMemberSemantic.Introduction, this.BuilderData.ToRef() )];
                }

            case DeclarationKind.Operator:
                {
                    if ( finalMethod.OperatorKind.GetCategory() == OperatorCategory.Conversion )
                    {
                        Invariant.Assert( finalMethod.Parameters.Count == 1 );

                        var syntax = ConversionOperatorDeclaration(
                            AdviceSyntaxGenerator.GetAttributeLists( finalMethod, context ),
                            finalMethod.GetSyntaxModifierList(),
                            SyntaxFactoryEx.TokenWithTrailingSpace( finalMethod.OperatorKind.ToOperatorKeyword() ),
                            explicitInterfaceSpecifier,
                            SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.OperatorKeyword ),
                            context.SyntaxGenerator.TypeSyntax( finalMethod.ReturnType )
                                .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                            context.SyntaxGenerator.ParameterList( finalMethod, context.FinalCompilation ),
                            null,
                            !hasNoBody
                                ? ArrowExpressionClause( context.SyntaxGenerator.DefaultExpression( finalMethod.ReturnType ) )
                                : default,
                            Token( SyntaxKind.SemicolonToken ) );

                        return [new InjectedMember( this, syntax, this.AspectLayerId, InjectedMemberSemantic.Introduction, this.BuilderData.ToRef() )];
                    }
                    else
                    {
                        Invariant.Assert( finalMethod.Parameters.Count is 1 or 2 );

                        var syntax = OperatorDeclaration(
                            AdviceSyntaxGenerator.GetAttributeLists( finalMethod, context ),
                            finalMethod.GetSyntaxModifierList(),
                            context.SyntaxGenerator.TypeSyntax( finalMethod.ReturnType )
                                .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                            explicitInterfaceSpecifier,
                            SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.OperatorKeyword ),
                            SyntaxFactoryEx.TokenWithTrailingSpace( finalMethod.OperatorKind.ToOperatorKeyword() ),
                            context.SyntaxGenerator.ParameterList( finalMethod, context.FinalCompilation ),
                            null,
                            !hasNoBody
                                ? ArrowExpressionClause( context.SyntaxGenerator.DefaultExpression( finalMethod.ReturnType ) )
                                : default,
                            Token( SyntaxKind.SemicolonToken ) );

                        return [new InjectedMember( this, syntax, this.AspectLayerId, InjectedMemberSemantic.Introduction, this.BuilderData.ToRef() )];
                    }
                }

            default:
                {
                    // ReSharper disable RedundantLinebreak

                    // Interface method will be declared without any body.
                    // Async iterator can have empty body and still be in iterator, returning anything is invalid.
                    var blockBody =
                        hasNoBody
                            ? null
                            : syntaxGenerator.FormattedBlock(
                                !finalMethod.ReturnParameter.Type.IsConvertibleTo( typeof(void) )
                                    ? finalMethod.GetIteratorInfo().IsIteratorMethod == true
                                        ?
                                        [
                                            syntaxGenerator.FormattedBlock(
                                                YieldStatement(
                                                    SyntaxKind.YieldBreakStatement,
                                                    List<AttributeListSyntax>(),
                                                    SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.YieldKeyword ),
                                                    SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.BreakKeyword ),
                                                    null,
                                                    Token( TriviaList(), SyntaxKind.SemicolonToken, TriviaList() ) ) )
                                        ]
                                        :
                                        [
                                            ReturnStatement(
                                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                                                DefaultExpression( syntaxGenerator.TypeSyntax( finalMethod.ReturnParameter.Type ) ),
                                                Token( SyntaxKind.SemicolonToken ) )
                                        ]
                                    : [] );

                    // ReSharper enable RedundantLinebreak

                    var method =
                        MethodDeclaration(
                            AdviceSyntaxGenerator.GetAttributeLists( finalMethod, context ),
                            finalMethod.GetSyntaxModifierList(),
                            context.SyntaxGenerator.ReturnType( finalMethod ).WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                            explicitInterfaceSpecifier,
                            finalMethod.GetCleanName(),
                            context.SyntaxGenerator.TypeParameterList( finalMethod, context.FinalCompilation ),
                            context.SyntaxGenerator.ParameterList( finalMethod, context.FinalCompilation ),
                            context.SyntaxGenerator.ConstraintClauses( finalMethod ),
                            blockBody,
                            null,
                            hasNoBody ? Token( SyntaxKind.SemicolonToken ) : default );

                    return [new InjectedMember( this, method, this.AspectLayerId, InjectedMemberSemantic.Introduction, this.BuilderData.ToRef() )];
                }
        }
    }
}