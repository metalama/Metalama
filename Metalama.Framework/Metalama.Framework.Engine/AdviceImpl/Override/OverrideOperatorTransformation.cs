// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Templating.MetaModel;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

/// <summary>
/// Method override, which expands a template.
/// </summary>
internal sealed class OverrideOperatorTransformation : OverrideMemberTransformation
{
    private readonly IFullRef<IMethod> _targetOperator;

    private BoundTemplateMethod BoundTemplate { get; }

    public OverrideOperatorTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IMethod> targetOperator,
        BoundTemplateMethod boundTemplate )
        : base( aspectLayerInstance, targetOperator )
    {
        this._targetOperator = targetOperator;
        this.BoundTemplate = boundTemplate;
    }

    public override IFullRef<IMember> OverriddenDeclaration => this._targetOperator;

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var overriddenDeclaration = this._targetOperator.GetTarget( this.InitialCompilation );
        var proceedExpression = this.CreateProceedExpression( context, overriddenDeclaration );

        var metaApi = MetaApi.ForMethod(
            overriddenDeclaration,
            new MetaApiProperties(
                this.InitialCompilation,
                context.DiagnosticSink,
                this.BoundTemplate.TemplateMember.AsMemberOrNamedType(),
                this.AspectLayerId,
                context.SyntaxGenerationContext,
                this.AspectInstance,
                context.ServiceProvider,
                MetaApiStaticity.AlwaysStatic ) );

        var expansionContext = new TemplateExpansionContext(
            context,
            metaApi,
            overriddenDeclaration,
            this.BoundTemplate,
            _ => proceedExpression,
            this.AspectLayerId );

        var templateDriver = this.BoundTemplate.TemplateMember.Driver;

        if ( !templateDriver.TryExpandDeclaration(
                expansionContext,
                this.BoundTemplate.GetTemplateArgumentsForMethod( overriddenDeclaration ),
                out var newMethodBody ) )
        {
            // Template expansion error.
            return [];
        }

        var modifiers = overriddenDeclaration
            .GetSyntaxModifierList( ModifierCategories.Static | ModifierCategories.Unsafe )
            .Insert( 0, SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) );

        var syntax =
            MethodDeclaration(
                List<AttributeListSyntax>(),
                TokenList( modifiers ),
                context.SyntaxGenerator.ReturnType( overriddenDeclaration )
                    .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                null,
                Identifier(
                    context.InjectionNameProvider.GetOverrideName(
                        overriddenDeclaration.DeclaringType,
                        this.AspectLayerId,
                        overriddenDeclaration ) ),
                null,
                context.SyntaxGenerator.ParameterList( overriddenDeclaration, context.FinalCompilation, removeDefaultValues: true ),
                List<TypeParameterConstraintClauseSyntax>(),
                newMethodBody,
                null );

        return [new InjectedMember( this, syntax, this.AspectLayerId, InjectedMemberSemantic.Override, overriddenDeclaration.ToFullRef() )];
    }

    private SyntaxUserExpression CreateProceedExpression( MemberInjectionContext context, IMethod overriddenDeclaration )
    {
        return new SyntaxUserExpression(
            context.AspectReferenceSyntaxProvider.GetOperatorReference(
                this.AspectLayerId,
                overriddenDeclaration,
                context.SyntaxGenerator ),
            overriddenDeclaration.ReturnType );
    }
}