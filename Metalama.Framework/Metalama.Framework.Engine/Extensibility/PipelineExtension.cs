// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// Represents something that extends the Metalama pipeline.
/// </summary>
public abstract class PipelineExtension
{
    public abstract bool Initialize( PipelineExtensionInitializationContext context );

    /// <summary>
    /// Executes any relevant <see cref="!:IPipelineContributor" />. This method is invoked as soon as the contributors have
    /// been collected. When the source is an aspect, the method is invoked right after the aspect builder has executed.
    /// When the source is a fabric, it is executed right after the initial compilation is created, before the pipeline steps are executed.
    /// </summary>
    public virtual Task ExecuteContributorsAsync(
        AspectPipelineConfiguration pipelineConfiguration,
        CompilationModel initialCompilation,
        UserDiagnosticSink diagnosticSink,
        ImmutableArray<IPipelineContributor> contributors,
        CancellationToken cancellationToken )
        => Task.CompletedTask;

    /// <summary>
    /// Gets a list of <see cref="!:ITransitiveAspectsManifestExtension" /> given a list of <see cref="!:ITransitivePipelineContributor" />.
    /// </summary>
    /// <param name="contributors"></param>
    /// <returns></returns>
    public virtual IEnumerable<ITransitiveAspectsManifestExtension> GetTransitiveManifestExtensions( IEnumerable<ITransitivePipelineContributor> contributors )
        => [];

    public virtual IEnumerable<IExtensionPipelineContributor> GetPipelineContributorsFromTransitiveManifest(
        ImmutableArray<ITransitiveAspectsManifestExtension> extensions )
        => [];

    public virtual Task<ExtensionPipelineContributorsResult> ExecutePipelineContributorsAsync(
        AspectPipelineConfiguration pipelineConfiguration,
        IEnumerable<IPipelineContributor> contributors,
        CompilationModel initialCompilation,
        CompilationModel finalCompilation,
        CancellationToken cancellationToken )
        => Task.FromResult( ExtensionPipelineContributorsResult.Empty );

    public virtual Task<ExtensionPipelineContributorsResult> ExecuteDesignTimePipelineContributorsAsync(
        AspectPipelineConfiguration pipelineConfiguration,
        IEnumerable<IPipelineContributor> contributors,
        CompilationModel initialCompilation,
        CompilationModel finalCompilation,
        CancellationToken cancellationToken )
        => Task.FromResult( ExtensionPipelineContributorsResult.Empty );

    /// <summary>
    /// Method invoked at design time out-of-pipeline by the Analyzer. It must report any diagnostic supported by the extension.
    /// </summary>
    public virtual ImmutableUserDiagnosticList AnalyzeSemanticModel(
        AspectPipelineConfiguration pipelineConfiguration,
        SemanticModel semanticModel,
        DesignTimeAspectPipelineResultExtensionCollection extensions,
        AspectRepository aspectRepository,
        CancellationToken cancellationToken )
        => ImmutableUserDiagnosticList.Empty;
}