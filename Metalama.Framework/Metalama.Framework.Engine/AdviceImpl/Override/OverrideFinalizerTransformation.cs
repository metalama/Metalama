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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

/// <summary>
/// Finalizer override, which expands a template.
/// </summary>
internal sealed class OverrideFinalizerTransformation : OverrideMemberTransformation
{
    private readonly IFullRef<IMethod> _targetFinalizer;

    private BoundTemplateMethod BoundTemplate { get; }

    public OverrideFinalizerTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IMethod> targetFinalizer,
        BoundTemplateMethod boundTemplate )
        : base( aspectLayerInstance, targetFinalizer )
    {
        this._targetFinalizer = targetFinalizer;
        this.BoundTemplate = boundTemplate;
    }

    public override IFullRef<IMember> OverriddenDeclaration => this._targetFinalizer;

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var proceedExpression = this.CreateProceedExpression( context );

        var overriddenDeclaration = this._targetFinalizer.GetTarget( this.InitialCompilation );

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
                MetaApiStaticity.Default ) );

        var expansionContext = new TemplateExpansionContext(
            context,
            metaApi,
            overriddenDeclaration,
            this.BoundTemplate,
            _ => proceedExpression,
            this.AspectLayerId );

        var templateDriver = this.BoundTemplate.TemplateMember.Driver;

        if ( !templateDriver.TryExpandDeclaration( expansionContext, this.BoundTemplate.TemplateArguments, out var newMethodBody ) )
        {
            // Template expansion error.
            return [];
        }

        var modifiers = overriddenDeclaration
            .GetSyntaxModifierList( ModifierCategories.Unsafe )
            .Insert( 0, SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) );

        var syntax =
            MethodDeclaration(
                List<AttributeListSyntax>(),
                TokenList( modifiers ),
                PredefinedType( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.VoidKeyword ) ),
                null,
                Identifier(
                    context.InjectionNameProvider.GetOverrideName(
                        overriddenDeclaration.DeclaringType,
                        this.AspectLayerId,
                        overriddenDeclaration ) ),
                null,
                ParameterList(),
                List<TypeParameterConstraintClauseSyntax>(),
                newMethodBody,
                null );

        return [new InjectedMember( this, syntax, this.AspectLayerId, InjectedMemberSemantic.Override, overriddenDeclaration.ToFullRef() )];
    }

    private SyntaxUserExpression CreateProceedExpression( MemberInjectionContext context )
        => new(
            context.AspectReferenceSyntaxProvider.GetFinalizerReference( this.AspectLayerId ),
            context.FinalCompilation.Cache.SystemVoidType );

    public override TransformationObservability Observability => TransformationObservability.None;
}