// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal abstract class OverrideIndexerBaseTransformation : OverridePropertyOrIndexerTransformation
{
    protected OverrideIndexerBaseTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IIndexer> overriddenDeclaration )
        : base( aspectLayerInstance, overriddenDeclaration )
    {
        this.OverriddenIndexer = overriddenDeclaration;
    }

    protected IFullRef<IIndexer> OverriddenIndexer { get; }

    public override IFullRef<IMember> OverriddenDeclaration => this.OverriddenIndexer;

    protected IEnumerable<InjectedMember> GetInjectedMembersImpl(
        MemberInjectionContext context,
        BlockSyntax? getAccessorBody,
        BlockSyntax? setAccessorBody )
    {
        var overriddenDeclaration = this.OverriddenIndexer.GetTarget( this.InitialCompilation );

        var setAccessorDeclarationKind =
            overriddenDeclaration.Writeability is Writeability.InitOnly or Writeability.ConstructorOnly
                ? SyntaxKind.InitAccessorDeclaration
                : SyntaxKind.SetAccessorDeclaration;

        var modifiers = overriddenDeclaration
            .GetSyntaxModifierList( ModifierCategories.Unsafe )
            .Insert( 0, SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) );

        var overrides = new[]
        {
            new InjectedMember(
                this,
                IndexerDeclaration(
                    List<AttributeListSyntax>(),
                    TokenList( modifiers ),
                    context.SyntaxGenerator.IndexerType( overriddenDeclaration )
                        .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                    null,
                    Token( SyntaxKind.ThisKeyword ),
                    TransformationHelper.GetIndexerOverrideParameterList(
                        context.FinalCompilation,
                        context.SyntaxGenerationContext,
                        overriddenDeclaration,
                        context.InjectionNameProvider.GetOverriddenByType(
                            this.AspectInstance,
                            overriddenDeclaration,
                            context.SyntaxGenerationContext ) ),
                    AccessorList(
                        List(
                            new[]
                                {
                                    getAccessorBody != null
                                        ? AccessorDeclaration(
                                            SyntaxKind.GetAccessorDeclaration,
                                            List<AttributeListSyntax>(),
                                            default,
                                            getAccessorBody )
                                        : null,
                                    setAccessorBody != null
                                        ? AccessorDeclaration(
                                            setAccessorDeclarationKind,
                                            List<AttributeListSyntax>(),
                                            default,
                                            setAccessorBody )
                                        : null
                                }.Where( a => a != null )
                                .AssertNoneNull() ) ),
                    null,
                    default ),
                this.AspectLayerId,
                InjectedMemberSemantic.Override,
                overriddenDeclaration.ToFullRef() )
        };

        return overrides;
    }

    protected SyntaxUserExpression CreateProceedDynamicExpression(
        MemberInjectionContext context,
        IMethod accessor,
        TemplateKind templateKind,
        IIndexer overriddenDeclaration )
        => accessor.MethodKind switch
        {
            MethodKind.PropertyGet => ProceedHelper.CreateProceedDynamicExpression(
                context.SyntaxGenerationContext,
                this.CreateProceedGetExpression( context ),
                templateKind,
                overriddenDeclaration.GetMethod.AssertNotNull() ),
            MethodKind.PropertySet => new SyntaxUserExpression(
                this.CreateProceedSetExpression( context ),
                overriddenDeclaration.Compilation.GetCompilationModel().Cache.SystemVoidType ),
            _ => throw new AssertionFailedException( $"Unexpected MethodKind for '{accessor}': {accessor.MethodKind}." )
        };

    protected override ExpressionSyntax CreateProceedGetExpression( MemberInjectionContext context )
        => TransformationHelper.CreateIndexerProceedGetExpression(
            context.AspectReferenceSyntaxProvider,
            context.SyntaxGenerationContext,
            this.OverriddenIndexer.GetTarget( context.FinalCompilation ),
            this.AspectLayerId );

    protected override ExpressionSyntax CreateProceedSetExpression( MemberInjectionContext context )
        => TransformationHelper.CreateIndexerProceedSetExpression(
            context.AspectReferenceSyntaxProvider,
            context.SyntaxGenerationContext,
            this.OverriddenIndexer.GetTarget( context.FinalCompilation ),
            this.AspectLayerId );
}