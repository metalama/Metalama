// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceStaticConstructorTransformation : IntroduceMemberTransformation<ConstructorBuilderData>, IReplaceMemberTransformation
{
    public IntroduceStaticConstructorTransformation( AspectLayerInstance aspectLayerInstance, ConstructorBuilderData introducedDeclaration ) : base(
        aspectLayerInstance,
        introducedDeclaration )
    {
        Invariant.Assert( introducedDeclaration.IsStatic );
    }

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var constructorBuilder = this.BuilderData.ToRef().GetTarget( context.FinalCompilation );

        var syntax =
            ConstructorDeclaration(
                AdviceSyntaxGenerator.GetAttributeLists( constructorBuilder, context ),
                TokenList( Token( TriviaList(), SyntaxKind.StaticKeyword, TriviaList( Space ) ) ),
                Identifier( constructorBuilder.DeclaringType.Name ),
                ParameterList(),
                null,
                context.SyntaxGenerator.FormattedBlock().WithGeneratedCodeAnnotation( this.AspectInstance.AspectClass.GeneratedCodeAnnotation ),
                null );

        return
        [
            new InjectedMember(
                this,
                syntax,
                this.AspectLayerId,
                InjectedMemberSemantic.Introduction,
                this.BuilderData.ToRef() )
        ];
    }

    public IFullRef<IMember>? ReplacedMember => this.BuilderData.ReplacedImplicitConstructor;

    public override InsertPosition InsertPosition => this.ReplacedMember?.ToInsertPosition() ?? this.BuilderData.InsertPosition;

    public override TransformationObservability Observability => TransformationObservability.CompileTimeOnly;

    public override FormattableString ToDisplayString() => $"Introduce a static constructor into '{this.TargetDeclaration}'.";
}