// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Introspection;
using System;

namespace Metalama.Framework.Engine.Transformations;

internal abstract class BaseTransformation : ITransformation
{
    protected BaseTransformation( AspectLayerInstance aspectLayerInstance )
    {
        // Don't keep a reference to the Advice, as it's supposed to be short-lived.
        this.AspectLayerInstance = aspectLayerInstance;
    }

    public AspectLayerId AspectLayerId => this.AspectLayerInstance.AspectLayerId;

    public IAspectInstanceInternal AspectInstance => this.AspectLayerInstance.AspectInstance;

    /// <summary>
    /// Gets the <see cref="CompilationModel"/> on which the templates should be executed.
    /// </summary>
    protected CompilationModel InitialCompilation => this.AspectLayerInstance.InitialCompilation;

    IRef<IDeclaration> ITransformationBase.TargetDeclaration => this.TargetDeclaration;
    
    /// <summary>
    /// Gets the declaration that is transformed, or the declaration into which a new declaration is being introduced. 
    /// </summary>
    public abstract IFullRef<IDeclaration> TargetDeclaration { get; }

    IAspectClass ITransformationBase.AspectClass => this.AspectInstance.AspectClass;

    public AspectLayerInstance AspectLayerInstance { get; }

    public int OrderWithinPipelineStepAndTypeAndAspectInstance { get; set; }

    public int OrderWithinPipelineStepAndType { get; set; }

    public int OrderWithinPipeline { get; set; }

    public abstract TransformationObservability Observability { get; }

    public abstract IntrospectionTransformationKind TransformationKind { get; }

    public abstract FormattableString ToDisplayString();
}