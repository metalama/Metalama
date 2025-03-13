// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceFieldTransformation : IntroduceMemberTransformation<FieldBuilderData>
{
    private readonly TemplateMember<IField>? _template;

    public IntroduceFieldTransformation(
        AspectLayerInstance aspectLayerInstance,
        FieldBuilderData introducedDeclaration,
        TemplateMember<IField>? template ) : base(
        aspectLayerInstance,
        introducedDeclaration )
    {
        this._template = template;
    }

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var syntaxGenerator = context.SyntaxGenerationContext.SyntaxGenerator;
        var fieldBuilder = this.BuilderData.ToRef().GetTarget( context.FinalCompilation );

        // If template fails to expand, we will still generate the field, albeit without the initializer.
        _ = AdviceSyntaxGenerator.GetInitializerExpressionOrMethod(
            fieldBuilder,
            this.AspectLayerInstance,
            context,
            fieldBuilder.Type,
            fieldBuilder.InitializerExpression,
            this._template?.GetInitializerTemplate(),
            out var initializerExpression,
            out var initializerMethod );

        // If we are introducing a field into a struct in C# 10, it must have an explicit default value.
        if ( initializerExpression == null
             && fieldBuilder.DeclaringType.TypeKind is TypeKind.Struct or TypeKind.RecordStruct
             && context.SyntaxGenerationContext.RequiresStructFieldInitialization )
        {
            initializerExpression = SyntaxFactoryEx.Default;
        }

        var field =
            FieldDeclaration(
                AdviceSyntaxGenerator.GetAttributeLists( fieldBuilder, context ),
                fieldBuilder.GetSyntaxModifierList(),
                VariableDeclaration(
                    syntaxGenerator.TypeSyntax( fieldBuilder.Type )
                        .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier( fieldBuilder.Name ),
                            null,
                            initializerExpression != null
                                ? EqualsValueClause( initializerExpression )
                                : null ) ) ) );

        if ( initializerMethod != null )
        {
            return
            [
                new InjectedMember( this, field, this.AspectLayerId, InjectedMemberSemantic.Introduction, this.BuilderData.ToRef() ),
                new InjectedMember(
                    this,
                    initializerMethod,
                    this.AspectLayerId,
                    InjectedMemberSemantic.InitializerMethod,
                    this.BuilderData.ToRef() )
            ];
        }
        else
        {
            return [new InjectedMember( this, field, this.AspectLayerId, InjectedMemberSemantic.Introduction, this.BuilderData.ToRef() )];
        }
    }
}