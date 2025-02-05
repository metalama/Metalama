// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.Pipeline.DesignTime;

public abstract class BaseDesignTimeAspectPipeline : AspectPipeline
{
    protected BaseDesignTimeAspectPipeline( ServiceProvider<IProjectService> serviceProvider )
        : base( serviceProvider, ExecutionScenario.DesignTime ) { }

    protected sealed override SyntaxGenerationOptions GetSyntaxGenerationOptions() => SyntaxGenerationOptions.Formatted;

    /// <inheritdoc/>
    private protected override HighLevelPipelineStage CreateHighLevelStage(
        PipelineStageConfiguration configuration,
        CompileTimeProject compileTimeProject )
        => new DesignTimePipelineStage( configuration.AspectLayers );

    private protected override LowLevelPipelineStage? CreateLowLevelStage( PipelineStageConfiguration configuration ) => null;
}