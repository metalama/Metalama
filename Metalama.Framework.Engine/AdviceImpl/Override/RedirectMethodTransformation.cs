// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

/// <summary>
/// Represents a method override, which redirects to another method without requiring template expansion.
/// </summary>
internal sealed class RedirectMethodTransformation( Advice advice, IFullRef<IMethod> overriddenMethod, IFullRef<IMethod> targetMethod )
    : OverrideMemberTransformation( advice.AspectLayerInstance, overriddenMethod )
{
    public override IFullRef<IMember> OverriddenDeclaration => overriddenMethod;

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var overriddenDeclaration = overriddenMethod.GetTarget( this.InitialCompilation );

        var body =
            context.SyntaxGenerationContext.SyntaxGenerator.FormattedBlock(
                overriddenDeclaration.ReturnType.SpecialType != SpecialType.Void
                    ? ReturnStatement(
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                        GetInvocationExpression(),
                        Token( SyntaxKind.SemicolonToken ) )
                    : ExpressionStatement( GetInvocationExpression() ) );

        return
        [
            new InjectedMember(
                this,
                MethodDeclaration(
                    List<AttributeListSyntax>(),
                    overriddenDeclaration.GetSyntaxModifierList(),
                    context.SyntaxGenerator.ReturnType( overriddenDeclaration )
                        .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                    null,
                    Identifier(
                        context.InjectionNameProvider.GetOverrideName(
                            overriddenDeclaration.DeclaringType,
                            this.AspectLayerId,
                            overriddenDeclaration ) ),
                    context.SyntaxGenerator.TypeParameterList( overriddenDeclaration, context.FinalCompilation ),
                    context.SyntaxGenerator.ParameterList( overriddenDeclaration, context.FinalCompilation, removeDefaultValues: true ),
                    context.SyntaxGenerator.ConstraintClauses( overriddenDeclaration ),
                    body,
                    null ),
                this.AspectLayerId,
                InjectedMemberSemantic.Override,
                overriddenDeclaration.ToFullRef() )
        ];

        ExpressionSyntax GetInvocationExpression()
        {
            return
                InvocationExpression(
                    GetInvocationTargetExpression(),
                    ArgumentList( SeparatedList( overriddenDeclaration.Parameters.SelectAsReadOnlyList( p => Argument( IdentifierName( p.Name ) ) ) ) ) );
        }

        ExpressionSyntax GetInvocationTargetExpression()
        {
            var expression =
                overriddenDeclaration.IsStatic
                    ? (ExpressionSyntax) IdentifierName( targetMethod.Name.AssertNotNull() )
                    : MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        IdentifierName( targetMethod.Name.AssertNotNull() ) );

            return expression
                .WithAspectReferenceAnnotation( this.AspectLayerId, AspectReferenceOrder.Previous );
        }
    }
}