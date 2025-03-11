// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.AdditionalOutputs;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline.CompileTime
{
    /// <summary>
    /// The implementation of <see cref="HighLevelPipelineStage"/> used at compile time (not at design time).
    /// </summary>
    internal sealed class LinkerPipelineStage : HighLevelPipelineStage
    {
        public LinkerPipelineStage( IReadOnlyList<OrderedAspectLayer> aspectLayers )
            : base( aspectLayers ) { }

        /// <inheritdoc/>
        protected override async Task<AspectPipelineResult> GetStageResultAsync(
            AspectPipelineConfiguration pipelineConfiguration,
            AspectPipelineResult input,
            PipelineStepsResult pipelineStepsResult,
            TestableCancellationToken cancellationToken )
        {
            var initialCompilation = pipelineStepsResult.FirstCompilation;
            var finalCompilation = pipelineStepsResult.LastCompilation;

            // TODO: validators should not run here but after all pipeline stages. If there are several high-level stages, they may run several times.
            // Run the validators.
            var extensions = pipelineConfiguration.ServiceProvider.GetRequiredService<PipelineExtensionProvider>().Extensions;
            var pipelineContributorsResult = ExtensionPipelineContributorsResult.Empty;

            foreach ( var extension in extensions )
            {
                pipelineContributorsResult = pipelineContributorsResult.Concat(
                    await extension.ExecutePipelineContributorsAsync(
                        pipelineConfiguration,
                        pipelineStepsResult.ExtensionContributors,
                        initialCompilation,
                        finalCompilation,
                        cancellationToken ) );
            }

            // Run the linker.
            var linker = new AspectLinker(
                pipelineConfiguration.ServiceProvider,
                new AspectLinkerInput(
                    input.FirstCompilationModel.AssertNotNull(),
                    pipelineStepsResult.LastCompilation,
                    pipelineStepsResult.Transformations,
                    input.AspectLayers ) );

            var linkerResult = await linker.ExecuteAsync( cancellationToken );

            // Generate additional output files.
            var projectOptions = pipelineConfiguration.ServiceProvider.GetService<IProjectOptions>();
            IReadOnlyList<AdditionalCompilationOutputFile>? additionalCompilationOutputFiles = null;

            if ( projectOptions is { IsDesignTimeEnabled: false } )
            {
                additionalCompilationOutputFiles = await GenerateAdditionalCompilationOutputFilesAsync(
                    pipelineConfiguration.ServiceProvider,
                    input,
                    pipelineStepsResult,
                    cancellationToken );
            }

            // Return the result.
            return
                new AspectPipelineResult(
                    linkerResult.Compilation,
                    input.Project,
                    input.AspectLayers,
                    input.FirstCompilationModel.AssertNotNull(),
                    null,
                    input.Configuration,
                    input.Diagnostics.Concat( pipelineStepsResult.Diagnostics )
                        .Concat( linkerResult.Diagnostics )
                        .Concat( pipelineContributorsResult.Diagnostics ),
                    new PipelineContributorSources( input.ContributorSources.Contributors.Add( pipelineStepsResult.OverflowAspectSource ) ),
                    input.ExternallyInheritableAspects.AddRange(
                        pipelineStepsResult.InheritableAspectInstances.SelectAsReadOnlyCollection( i => new InheritableAspectInstance( i ) ) ),
                    finalCompilation.Annotations,
                    pipelineContributorsResult.TransitiveContributors,
                    additionalCompilationOutputFiles: additionalCompilationOutputFiles != null
                        ? input.AdditionalCompilationOutputFiles.AddRange( additionalCompilationOutputFiles )
                        : input.AdditionalCompilationOutputFiles,
                    aspectInstanceResults: input.AspectInstanceResults.AddRange( pipelineStepsResult.AspectInstanceResults ) );
        }

        private static async Task<IReadOnlyList<AdditionalCompilationOutputFile>> GenerateAdditionalCompilationOutputFilesAsync(
            ProjectServiceProvider serviceProvider,
            AspectPipelineResult input,
            PipelineStepsResult pipelineStepResult,
            TestableCancellationToken cancellationToken )
        {
            var generatedFiles = new List<AdditionalCompilationOutputFile>();

            // TODO: We don't need these diagnostics, but we cannot pass NullDiagnosticAdder here.
            var diagnostics = new UserDiagnosticSink( serviceProvider );

            var additionalSyntaxTrees = await DesignTimeSyntaxTreeGenerator.GenerateDesignTimeSyntaxTreesAsync(
                serviceProvider,
                input.LastCompilation,
                pipelineStepResult.FirstCompilation,
                pipelineStepResult.LastCompilation,
                pipelineStepResult.Transformations,
                diagnostics,
                cancellationToken );

            // Ignore diagnostics, because these will be coming from the analyzer.
            var uniquePaths = new HashSet<string>();

            foreach ( var syntaxTree in additionalSyntaxTrees )
            {
                var path = Path.GetDirectoryName( syntaxTree.Name )!;
                var name = Path.GetFileNameWithoutExtension( syntaxTree.Name );
                var ext = Path.GetExtension( syntaxTree.Name );
                var relativePath = Path.Combine( path, $"{name}.g{ext}" );
                relativePath = GetUniqueFilename( relativePath );

                generatedFiles.Add(
                    new GeneratedAdditionalCompilationOutputFile(
                        relativePath,
                        AdditionalCompilationOutputFileKind.DesignTimeGeneratedCode,
                        stream =>
                        {
                            using var writer = new StreamWriter( stream, syntaxTree.GeneratedSyntaxTree.Encoding ?? Encoding.UTF8 );
                            writer.Write( syntaxTree.GeneratedSyntaxTree.ToString() );
                        } ) );
            }

            generatedFiles.Add(
                new GeneratedAdditionalCompilationOutputFile(
                    "touch",
                    AdditionalCompilationOutputFileKind.DesignTimeTouch,
                    stream =>
                    {
                        using var writer = new StreamWriter( stream, Encoding.UTF8 );
                        writer.Write( Guid.NewGuid() );
                    } ) );

            return generatedFiles;

            string GetUniqueFilename( string filename )
            {
                if ( !uniquePaths.Add( filename ) )
                {
                    for ( var i = 1; /* Intentionally empty */; i++ )
                    {
                        var path = Path.GetDirectoryName( filename )!;
                        var name = Path.GetFileNameWithoutExtension( filename );
                        var ext = Path.GetExtension( filename );
                        var relativePath = Path.Combine( path, $"{name}.g.{i}{ext}" );

                        if ( uniquePaths.Add( relativePath ) )
                        {
                            return relativePath;
                        }
                    }
                }

                return filename;
            }
        }
    }
}