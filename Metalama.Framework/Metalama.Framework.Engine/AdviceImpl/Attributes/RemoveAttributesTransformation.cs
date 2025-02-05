// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Attributes;

internal sealed class RemoveAttributesTransformation : BaseSyntaxTreeTransformation, ITransformation
{
    public IFullRef<INamedType> AttributeType { get; }

    public RemoveAttributesTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IDeclaration> targetDeclaration,
        IFullRef<INamedType> attributeType ) : base( aspectLayerInstance, targetDeclaration )
    {
        this.AttributeType = attributeType;
        this.ContainingDeclaration = targetDeclaration;
    }

    public IFullRef<IDeclaration> ContainingDeclaration { get; }

    public override IFullRef<IDeclaration> TargetDeclaration => this.ContainingDeclaration;

    public override TransformationObservability Observability => TransformationObservability.CompileTimeOnly;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.RemoveAttributes;

    public override FormattableString ToDisplayString() => $"Remove attributes of type '{this.AttributeType}' from '{this.TargetDeclaration}'";
}