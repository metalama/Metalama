// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline.CompileTime
{
    /// <summary>
    /// A <see cref="HighLevelPipelineStage"/> for the WPF MarkupCompilePass1 temporary assembly. Reuses the full compile-time
    /// front-end (aspect discovery, eligibility, advice, template expansion of observable members) but skips the linker —
    /// the temp assembly only needs aspect-introduced member signatures so the XAML compiler can resolve type references.
    /// Emits the same partial-class stubs the design-time source generator produces.
    /// </summary>
    internal sealed class WpfPrecompilePipelineStage : HighLevelPipelineStage
    {
        public WpfPrecompilePipelineStage( IReadOnlyList<OrderedAspectLayer> aspectLayers )
            : base( aspectLayers ) { }

        protected override async Task<AspectPipelineResult> GetStageResultAsync(
            AspectPipelineConfiguration pipelineConfiguration,
            AspectPipelineResult input,
            PipelineStepsResult pipelineStepsResult,
            TestableCancellationToken cancellationToken )
        {
            var diagnosticSink = new UserDiagnosticSink( pipelineConfiguration.ServiceProvider );

            var introducedSyntaxTrees = await DesignTimeSyntaxTreeGenerator.GenerateDesignTimeSyntaxTreesAsync(
                pipelineConfiguration.ServiceProvider,
                input.LastCompilation,
                pipelineStepsResult.FirstCompilation,
                pipelineStepsResult.LastCompilation,
                pipelineStepsResult.Transformations,
                diagnosticSink,
                cancellationToken );

            var addTransformations = introducedSyntaxTrees.SelectAsArray( t => SyntaxTreeTransformation.AddTree( t.GeneratedSyntaxTree.WithFilePath( t.Name ) ) );

            var compilationWithStubs = input.LastCompilation.Update( addTransformations );

            return new AspectPipelineResult(
                compilationWithStubs,
                input.Project,
                input.AspectLayers,
                input.FirstCompilationModel.AssertNotNull(),
                null,
                input.Configuration,
                input.Diagnostics
                    .Concat( pipelineStepsResult.Diagnostics )
                    .Concat( diagnosticSink.ToImmutable() ),
                new PipelineContributorSources( input.ContributorSources.Contributors.Add( pipelineStepsResult.OverflowAspectSource ) ),
                input.ExternallyInheritableAspects.AddRange(
                    pipelineStepsResult.InheritableAspectInstances.SelectAsReadOnlyCollection( i => new InheritableAspectInstance( i ) ) ),
                pipelineStepsResult.LastCompilation.Annotations,
                input.TransitiveContributors,
                additionalSyntaxTrees: input.AdditionalSyntaxTrees,
                aspectInstanceResults: input.AspectInstanceResults.AddRange( pipelineStepsResult.AspectInstanceResults ),
                additionalCompilationOutputFiles: input.AdditionalCompilationOutputFiles,
                transformations: pipelineStepsResult.Transformations.ToImmutableArray<ITransformationBase>() );
        }
    }
}
