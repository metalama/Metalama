// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.CodeModel;

public readonly struct AnnotationInstance
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IRef<IDeclaration> TargetDeclaration { get; }

    public IAnnotation Annotation { get; }

    public bool Export { get; }

    internal AnnotationInstance( IAnnotation annotation, bool export, IRef<IDeclaration> targetDeclaration )
    {
        this.Annotation = annotation;
        this.Export = export;
        this.TargetDeclaration = targetDeclaration;
    }
}