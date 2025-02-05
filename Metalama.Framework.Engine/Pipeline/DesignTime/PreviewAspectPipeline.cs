// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Linq;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline.DesignTime;

public sealed class PreviewAspectPipeline : AspectPipeline
{
    public PreviewAspectPipeline( ProjectServiceProvider serviceProvider, ExecutionScenario executionScenario )
        : base( serviceProvider, executionScenario ) { }

    private protected override LowLevelPipelineStage CreateLowLevelStage( PipelineStageConfiguration configuration )
    {
        var partData = configuration.AspectLayers.Single();

        return new LowLevelPipelineStage( configuration.Weaver!, partData.AspectClass );
    }

    protected override SyntaxGenerationOptions GetSyntaxGenerationOptions() => SyntaxGenerationOptions.Formatted;

    private protected override HighLevelPipelineStage CreateHighLevelStage(
        PipelineStageConfiguration configuration,
        CompileTimeProject compileTimeProject )
        => new LinkerPipelineStage( configuration.AspectLayers );

    public async Task<FallibleResult<PartialCompilation>> ExecutePreviewAsync(
        DiagnosticBag diagnostics,
        PartialCompilation compilation,
        AspectPipelineConfiguration configuration,
        TestableCancellationToken cancellationToken )
    {
        var result = await this.ExecuteAsync( compilation, diagnostics, configuration, cancellationToken );

        if ( result.IsSuccessful )
        {
            return result.Value.LastCompilation;
        }
        else
        {
            return FallibleResult<PartialCompilation>.Failed;
        }
    }
}