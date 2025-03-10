// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Attributes;

internal sealed class IntroduceAttributeTransformation : BaseSyntaxTreeTransformation, IIntroduceDeclarationTransformation
{
    public AttributeBuilderData BuilderData { get; }

    public IntroduceAttributeTransformation( AspectLayerInstance aspectLayerInstance, AttributeBuilderData builderData ) : base(
        aspectLayerInstance,
        builderData.ContainingDeclaration )
    {
        this.BuilderData = builderData;
    }

    public override IFullRef<IDeclaration> TargetDeclaration => this.BuilderData.ContainingDeclaration;

    public override TransformationObservability Observability => TransformationObservability.CompileTimeOnly;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.IntroduceAttribute;

    public DeclarationBuilderData DeclarationBuilderData => this.BuilderData;

    public override FormattableString ToDisplayString() => $"Introduce attribute of type '{this.BuilderData.Type}' into '{this.TargetDeclaration.Definition.ToDisplayString()}'";
}