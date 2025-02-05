// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceNamespaceTransformation : BaseTransformation, IIntroduceDeclarationTransformation
{
    private readonly NamespaceBuilderData _introducedDeclaration;

    public IntroduceNamespaceTransformation( AspectLayerInstance aspectLayerInstance, NamespaceBuilderData introducedDeclaration ) : base( aspectLayerInstance )
    {
        this._introducedDeclaration = introducedDeclaration.AssertNotNull();
    }

    public override TransformationObservability Observability => TransformationObservability.Always;

    DeclarationBuilderData IIntroduceDeclarationTransformation.DeclarationBuilderData => this._introducedDeclaration;

    public override IFullRef<IDeclaration> TargetDeclaration => this._introducedDeclaration.ContainingDeclaration.AssertNotNull();

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.IntroduceMember;

    public override FormattableString ToDisplayString() => $"Introduce {this._introducedDeclaration.DeclarationKind} '{this._introducedDeclaration}'.";
}