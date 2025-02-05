// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline.DesignTime
{
    /// <summary>
    /// An implementation of <see cref="DesignTimePipelineStage"/> called from source generators.
    /// </summary>
    internal sealed class DesignTimePipelineStage : HighLevelPipelineStage
    {
        public DesignTimePipelineStage( IReadOnlyList<OrderedAspectLayer> aspectLayers )
            : base( aspectLayers ) { }

        /// <inheritdoc/>
        protected override async Task<AspectPipelineResult> GetStageResultAsync(
            AspectPipelineConfiguration pipelineConfiguration,
            AspectPipelineResult input,
            PipelineStepsResult pipelineStepsResult,
            TestableCancellationToken cancellationToken )
        {
            var diagnosticSink = new UserDiagnosticSink( pipelineConfiguration.ServiceProvider );

            var extensionPipelineContributorsResult = ExtensionPipelineContributorsResult.Empty;

            if ( pipelineStepsResult.ExtensionContributors.Count > 0 )
            {
                foreach ( var pipelineExtension in pipelineConfiguration.Extensions )
                {
                    extensionPipelineContributorsResult = extensionPipelineContributorsResult.Concat(
                        await pipelineExtension.ExecuteDesignTimePipelineContributorsAsync(
                            pipelineConfiguration,
                            pipelineStepsResult.ExtensionContributors,
                            pipelineStepsResult.FirstCompilation,
                            pipelineStepsResult.LastCompilation,
                            cancellationToken ) );
                }
            }

            // Generate the additional syntax trees.

            var additionalSyntaxTrees = await DesignTimeSyntaxTreeGenerator.GenerateDesignTimeSyntaxTreesAsync(
                pipelineConfiguration.ServiceProvider,
                input.LastCompilation,
                pipelineStepsResult.FirstCompilation,
                pipelineStepsResult.LastCompilation,
                pipelineStepsResult.Transformations,
                diagnosticSink,
                cancellationToken );

            return
                new AspectPipelineResult(
                    input.LastCompilation,
                    input.Project,
                    input.AspectLayers,
                    input.FirstCompilationModel.AssertNotNull(),
                    pipelineStepsResult.LastCompilation,
                    input.Configuration,
                    input.Diagnostics.Concat( pipelineStepsResult.Diagnostics )
                        .Concat( diagnosticSink.ToImmutable() )
                        .Concat( extensionPipelineContributorsResult.Diagnostics ),
                    new PipelineContributorSources( input.ContributorSources.Contributors.Add( pipelineStepsResult.OverflowAspectSource ) ),
                    pipelineStepsResult.InheritableAspectInstances.ToImmutableArray(),
                    pipelineStepsResult.LastCompilation.Annotations,
                    extensionPipelineContributorsResult.TransitiveContributors,
                    input.AdditionalSyntaxTrees.AddRange( additionalSyntaxTrees ),
                    input.AspectInstanceResults.AddRange( pipelineStepsResult.AspectInstanceResults ),
                    transformations: pipelineStepsResult.Transformations.ToImmutableArray<ITransformationBase>() );
        }
    }
}