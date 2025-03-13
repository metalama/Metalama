// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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