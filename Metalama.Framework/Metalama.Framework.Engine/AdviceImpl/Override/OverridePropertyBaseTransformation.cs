// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal abstract class OverridePropertyBaseTransformation : OverridePropertyOrIndexerTransformation
{
    protected OverridePropertyBaseTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IProperty> overriddenProperty )
        : base( aspectLayerInstance, overriddenProperty )
    {
        this.OverriddenProperty = overriddenProperty;
    }
    
    protected IFullRef<IProperty> OverriddenProperty { get; }

    public override IFullRef<IMember> OverriddenDeclaration => this.OverriddenProperty;

    protected IEnumerable<InjectedMember> GetInjectedMembersImpl(
        MemberInjectionContext context,
        BlockSyntax? getAccessorBody,
        BlockSyntax? setAccessorBody )
    {
        var overriddenDeclaration = this.OverriddenProperty.GetTarget( this.InitialCompilation );

        var propertyName = context.InjectionNameProvider.GetOverrideName(
            overriddenDeclaration.DeclaringType,
            this.AspectLayerId,
            overriddenDeclaration );

        var setAccessorDeclarationKind = (overriddenDeclaration.IsStatic, overriddenDeclaration.Writeability) switch
        {
            (true, not Writeability.None) => SyntaxKind.SetAccessorDeclaration,
            (false, Writeability.ConstructorOnly) =>
                context.SyntaxGenerationContext.SupportsInitAccessors ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration,
            (false, Writeability.InitOnly) => SyntaxKind.InitAccessorDeclaration,
            (false, Writeability.All) => SyntaxKind.SetAccessorDeclaration,
            _ => SyntaxKind.None
        };

        var modifiers = overriddenDeclaration
            .GetSyntaxModifierList( ModifierCategories.Static | ModifierCategories.Unsafe )
            .Insert( 0, SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) );

        var overrides = new[]
        {
            new InjectedMember(
                this,
                SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers,
                    context.SyntaxGenerator.PropertyType( overriddenDeclaration )
                        .WithOptionalTrailingTrivia( SyntaxFactory.ElasticSpace, context.SyntaxGenerationContext.Options ),
                    null,
                    SyntaxFactory.Identifier( propertyName ),
                    SyntaxFactory.AccessorList(
                        SyntaxFactory.List(
                            new[]
                                {
                                    getAccessorBody != null
                                        ? SyntaxFactory.AccessorDeclaration(
                                            SyntaxKind.GetAccessorDeclaration,
                                            SyntaxFactory.List<AttributeListSyntax>(),
                                            default,
                                            getAccessorBody )
                                        : null,
                                    setAccessorBody != null
                                        ? SyntaxFactory.AccessorDeclaration(
                                            setAccessorDeclarationKind,
                                            SyntaxFactory.List<AttributeListSyntax>(),
                                            default,
                                            setAccessorBody )
                                        : null
                                }.Where( a => a != null )
                                .AssertNoneNull() ) ),
                    null,
                    null ),
                this.AspectLayerId,
                InjectedMemberSemantic.Override,
                overriddenDeclaration.ToFullRef() )
        };

        return overrides;
    }

    protected SyntaxUserExpression CreateProceedDynamicExpression( MemberInjectionContext context, IMethod accessor, TemplateKind templateKind )
        => accessor.MethodKind switch
        {
            MethodKind.PropertyGet => ProceedHelper.CreateProceedDynamicExpression(
                context.SyntaxGenerationContext,
                this.CreateProceedGetExpression( context ),
                templateKind,
                this.OverriddenProperty.GetTarget( context.FinalCompilation ).GetMethod.AssertNotNull() ),
            MethodKind.PropertySet => new SyntaxUserExpression(
                this.CreateProceedSetExpression( context ),
                context.FinalCompilation.Cache.SystemVoidType ),
            _ => throw new AssertionFailedException( $"Unexpected MethodKind for '{accessor}': {accessor.MethodKind}." )
        };

    protected override ExpressionSyntax CreateProceedGetExpression( MemberInjectionContext context )
        => TransformationHelper.CreatePropertyProceedGetExpression(
            context.AspectReferenceSyntaxProvider,
            context.SyntaxGenerationContext,
            this.OverriddenProperty.GetTarget( context.FinalCompilation ),
            this.AspectLayerId );

    protected override ExpressionSyntax CreateProceedSetExpression( MemberInjectionContext context )
        => TransformationHelper.CreatePropertyProceedSetExpression(
            context.AspectReferenceSyntaxProvider,
            context.SyntaxGenerationContext,
            this.OverriddenProperty.GetTarget( context.FinalCompilation ),
            this.AspectLayerId );
}