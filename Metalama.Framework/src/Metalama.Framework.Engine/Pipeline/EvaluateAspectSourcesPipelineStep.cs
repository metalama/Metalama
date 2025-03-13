// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// The <see cref="PipelineStage"/> that evaluates aspect sources and adds aspect instances to other steps. This step runs
/// in a fake depth numbered -1 because it needs to run before any other step within the aspect type.
/// </summary>
internal sealed class EvaluateAspectSourcesPipelineStep : PipelineStep
{
    private readonly ConcurrentLinkedList<IAspectSource> _aspectSources = new();

    public EvaluateAspectSourcesPipelineStep( PipelineStepsState parent, OrderedAspectLayer aspectLayer ) : base(
        parent,
        new PipelineStepId( aspectLayer.AspectLayerId, -1, -1, -1 ),
        aspectLayer ) { }

    public override async Task<CompilationModel> ExecuteAsync(
        CompilationModel compilation,
        UserDiagnosticSink diagnostics,
        int stepIndex,
        CancellationToken cancellationToken )
    {
        var aspectClass = this.AspectLayer.AspectClass;

        var concreteAspectInstances = await this.Parent.ExecuteAspectSourceAsync(
            compilation,
            aspectClass,
            this._aspectSources,
            diagnostics,
            cancellationToken );

        if ( concreteAspectInstances.IsDefaultOrEmpty )
        {
            return compilation;
        }
        else
        {
            return
                compilation.WithTransformationsAndAspectInstances( null, concreteAspectInstances, $"EvaluateAspectSource:{this.AspectLayer.AspectName}" );
        }
    }

    public void AddAspectSource( IAspectSource aspectSource ) => this._aspectSources.Add( aspectSource );
}