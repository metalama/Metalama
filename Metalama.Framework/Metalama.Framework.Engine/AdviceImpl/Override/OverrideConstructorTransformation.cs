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
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal sealed class OverrideConstructorTransformation : OverrideMemberTransformation
{
    private readonly IFullRef<IConstructor> _overriddenDeclaration;

    private BoundTemplateMethod Template { get; }

    public OverrideConstructorTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IConstructor> overriddenDeclaration,
        BoundTemplateMethod template )
        : base( aspectLayerInstance, overriddenDeclaration )
    {
        this._overriddenDeclaration = overriddenDeclaration;
        this.Template = template;
    }

    public override IFullRef<IMember> OverriddenDeclaration => this._overriddenDeclaration;

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var overriddenDeclaration = this._overriddenDeclaration.GetTarget( this.InitialCompilation );

        var proceedExpression = this.CreateProceedExpression( context, overriddenDeclaration );

        var metaApi = MetaApi.ForConstructor(
            overriddenDeclaration,
            new MetaApiProperties(
                this.InitialCompilation,
                context.DiagnosticSink,
                this.Template.TemplateMember.AsMemberOrNamedType(),
                this.AspectLayerId,
                context.SyntaxGenerationContext,
                this.AspectInstance,
                context.ServiceProvider,
                MetaApiStaticity.Default ) );

        var expansionContext = new TemplateExpansionContext(
            context,
            metaApi,
            overriddenDeclaration,
            this.Template,
            _ => proceedExpression,
            this.AspectLayerId );

        var templateDriver = this.Template.TemplateMember.Driver;

        if ( !templateDriver.TryExpandDeclaration( expansionContext, this.Template.TemplateArguments, out var newMethodBody ) )
        {
            // Template expansion error.
            return [];
        }

        var modifiers =
            overriddenDeclaration
                .GetSyntaxModifierList( ModifierCategories.Static | ModifierCategories.Unsafe )
                .Insert( 0, SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) );

        var syntax =
            overriddenDeclaration.IsStatic
                ? (MemberDeclarationSyntax) MethodDeclaration(
                    List<AttributeListSyntax>(),
                    TokenList( modifiers ),
                    PredefinedType( Token( SyntaxKind.VoidKeyword ) ),
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
                    null )
                : ConstructorDeclaration(
                    List<AttributeListSyntax>(),
                    TokenList( Token( TriviaList(), SyntaxKind.PrivateKeyword, TriviaList( ElasticSpace ) ) ),
                    Identifier( overriddenDeclaration.DeclaringType.Name ),
                    this.GetParameterList( context, overriddenDeclaration ),
                    ConstructorInitializer(
                        SyntaxKind.ThisConstructorInitializer,
                        ArgumentList( SeparatedList( overriddenDeclaration.Parameters.SelectAsArray( x => Argument( IdentifierName( x.Name ) ) ) ) ) ),
                    newMethodBody,
                    null );

        return [new InjectedMember( this, syntax, this.AspectLayerId, InjectedMemberSemantic.Override, overriddenDeclaration.ToFullRef() )];
    }

    private ParameterListSyntax GetParameterList( MemberInjectionContext context, IConstructor overriddenDeclaration )
    {
        var originalParameterList = context.SyntaxGenerator.ParameterList( overriddenDeclaration, context.FinalCompilation, true );

        var overriddenByParameterType = context.InjectionNameProvider.GetOverriddenByType(
            this.AspectInstance,
            overriddenDeclaration,
            context.SyntaxGenerationContext );

        return originalParameterList.WithAdditionalParameters( (overriddenByParameterType, AspectReferenceSyntaxProvider.LinkerOverrideParamName) );
    }

    private SyntaxUserExpression CreateProceedExpression( MemberInjectionContext context, IConstructor overriddenDeclaration )
        => new(
            overriddenDeclaration.IsStatic
                ? context.AspectReferenceSyntaxProvider.GetStaticConstructorReference( this.AspectLayerId )
                : context.AspectReferenceSyntaxProvider.GetConstructorReference(
                    this.AspectLayerId,
                    overriddenDeclaration,
                    context.SyntaxGenerator ),
            context.FinalCompilation.Cache.SystemVoidType );

    public override TransformationObservability Observability => TransformationObservability.None;
}