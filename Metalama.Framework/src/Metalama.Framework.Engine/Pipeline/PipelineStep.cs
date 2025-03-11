// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// A step executed by <see cref="HighLevelPipelineStage"/>.
/// </summary>
internal abstract class PipelineStep
{
    public PipelineStepId Id { get; }

    public OrderedAspectLayer AspectLayer { get; }

    protected PipelineStepsState Parent { get; }

    protected PipelineStep( PipelineStepsState parent, PipelineStepId id, OrderedAspectLayer aspectLayer )
    {
        this.Id = id;
        this.AspectLayer = aspectLayer;
        this.Parent = parent;
    }

    /// <summary>
    /// Executes the step.
    /// </summary>
    public abstract Task<CompilationModel> ExecuteAsync(
        CompilationModel compilation,
        UserDiagnosticSink diagnostics,
        int stepIndex,
        CancellationToken cancellationToken );

    public override string ToString() => this.Id.ToString();
}