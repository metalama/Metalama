// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Attributes;

internal sealed class AddAnnotationTransformation : BaseTransformation
{
    public AddAnnotationTransformation( AspectLayerInstance aspectLayerInstance, IFullRef<IDeclaration> declaration, AnnotationInstance annotationInstance ) : base(
        aspectLayerInstance )
    {
        this.TargetDeclaration = declaration;
        this.AnnotationInstance = annotationInstance;
    }

    public AnnotationInstance AnnotationInstance { get; }

    public override IFullRef<IDeclaration> TargetDeclaration { get; }

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.AddAnnotation;

    public override FormattableString ToDisplayString() => $"Adding annotation '{this.AnnotationInstance}' to '{this.TargetDeclaration}'.";

    public override TransformationObservability Observability => TransformationObservability.CompileTimeOnly;
}