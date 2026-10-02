// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
                var context = new DesignTimeContributorsContext(
                    pipelineConfiguration,
                    pipelineStepsResult.ExtensionContributors,
                    pipelineStepsResult.ExtensionContributorsAddedInStage,
                    pipelineStepsResult.FirstCompilation,
                    pipelineStepsResult.LastCompilation,
                    this.HighLevelStageIndex );

                foreach ( var pipelineExtension in pipelineConfiguration.Extensions )
                {
                    extensionPipelineContributorsResult = extensionPipelineContributorsResult.Concat(
                        await pipelineExtension.ExecuteDesignTimePipelineContributorsAsync( context, cancellationToken ) );
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

                    // The inheritable aspects and the transitive contributors of the earlier stages are kept, because the design-time pipeline
                    // reads them from the result of the last stage.
                    input.ExternallyInheritableAspects.AddRange( pipelineStepsResult.InheritableAspectInstances ),
                    pipelineStepsResult.LastCompilation.Annotations,
                    input.TransitiveContributors.AddRange( extensionPipelineContributorsResult.TransitiveContributors ),
                    input.AdditionalSyntaxTrees.AddRange( additionalSyntaxTrees ),
                    input.AspectInstanceResults.AddRange( pipelineStepsResult.AspectInstanceResults ),
                    transformations: pipelineStepsResult.Transformations.ToImmutableArray<ITransformationBase>() );
        }
    }
}