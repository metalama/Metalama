// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.AdviceImpl.Attributes;

internal sealed class AddAnnotationAdvice : Advice<AddAnnotationAdviceResult>
{
    private readonly AnnotationInstance _annotationInstance;

    public AddAnnotationAdvice( AdviceConstructorParameters parameters, AnnotationInstance annotationInstance )
        : base( parameters )
    {
        this._annotationInstance = annotationInstance;
    }

    public override AdviceKind AdviceKind => AdviceKind.AddAnnotation;

    protected override AddAnnotationAdviceResult Implement( in AdviceImplementationContext context )
    {
        context.AddTransformation( new AddAnnotationTransformation( this.AspectLayerInstance, this.TargetDeclaration.ToFullRef(), this._annotationInstance ) );

        return new AddAnnotationAdviceResult();
    }
}