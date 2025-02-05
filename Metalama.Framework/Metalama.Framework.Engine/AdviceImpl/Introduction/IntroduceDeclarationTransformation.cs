// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal abstract class IntroduceDeclarationTransformation<T> : BaseSyntaxTreeTransformation, IIntroduceDeclarationTransformation,
                                                                IInjectMemberTransformation
    where T : NamedDeclarationBuilderData
{
    public T BuilderData { get; }

    protected IntroduceDeclarationTransformation( AspectLayerInstance aspectLayerInstance, T introducedDeclaration ) : base(
        aspectLayerInstance,
        introducedDeclaration.PrimarySyntaxTree.AssertNotNull( "Introduced declarations must have a PrimarySyntaxTree assigned upfront." ) )
    {
        this.BuilderData = introducedDeclaration.AssertNotNull();
    }

    public abstract IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context );

    public virtual InsertPosition InsertPosition => this.BuilderData.InsertPosition;

    DeclarationBuilderData IIntroduceDeclarationTransformation.DeclarationBuilderData => this.BuilderData;

    public override IFullRef<IDeclaration> TargetDeclaration => this.BuilderData.ContainingDeclaration.AssertNotNull();

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.IntroduceMember;

    public override FormattableString ToDisplayString()
    {
        var containingDeclarationDefinition = this.BuilderData.ContainingDeclaration.Definition;

        return
            $"Introduce {this.BuilderData.DeclarationKind.ToDisplayString()} '{this.BuilderData.ToDisplayString( CodeDisplayFormat.MinimallyQualified )}' into {containingDeclarationDefinition.DeclarationKind.ToDisplayString()} '{containingDeclarationDefinition.ToDisplayString()}'.";
    }

    public override string ToString() => $"{{{this.GetType().Name} Builder={{{this.BuilderData}}}";
}