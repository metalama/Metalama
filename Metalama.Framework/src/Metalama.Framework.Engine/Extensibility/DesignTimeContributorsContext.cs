// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// Exposes the inputs of <see cref="PipelineExtension.ExecuteDesignTimePipelineContributorsAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// The design-time pipeline runs its high-level stages one after the other and accumulates the transitive contributors that the extensions return
/// in every stage. The contributors replayed from the contributor sources of the pipeline, for instance those of fabrics and of referenced
/// projects, are part of <see cref="Contributors"/> in every stage. An extension that returns transitive contributors must therefore process
/// <see cref="NewContributors"/>, so that the same contributor is not processed in two stages.
/// </para>
/// <para>
/// One instance is shared by all extensions of a stage. It references the compilations of the stage, so it must not be stored in an object
/// that outlives the call.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class DesignTimeContributorsContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DesignTimeContributorsContext"/> class.
    /// </summary>
    internal DesignTimeContributorsContext(
        AspectPipelineConfiguration pipelineConfiguration,
        IReadOnlyCollection<IExtensionPipelineContributor> contributors,
        IReadOnlyCollection<IExtensionPipelineContributor> contributorsAddedInStage,
        CompilationModel stageInitialCompilation,
        CompilationModel stageFinalCompilation,
        int highLevelStageIndex )
    {
        this.PipelineConfiguration = pipelineConfiguration;
        this.Contributors = contributors;
        this.ContributorsAddedInStage = contributorsAddedInStage;
        this.StageInitialCompilation = stageInitialCompilation;
        this.StageFinalCompilation = stageFinalCompilation;
        this.HighLevelStageIndex = highLevelStageIndex;
    }

    /// <summary>
    /// Gets the configuration of the pipeline.
    /// </summary>
    public AspectPipelineConfiguration PipelineConfiguration { get; }

    /// <summary>
    /// Gets the service provider of the project.
    /// </summary>
    public ProjectServiceProvider ServiceProvider => this.PipelineConfiguration.ServiceProvider;

    /// <summary>
    /// Gets all extension contributors of the stage: those replayed from the contributor sources of the pipeline, and those added by the aspects
    /// that executed in the stage.
    /// </summary>
    public IReadOnlyCollection<IExtensionPipelineContributor> Contributors { get; }

    /// <summary>
    /// Gets the extension contributors added by the aspects that executed in the stage.
    /// </summary>
    public IReadOnlyCollection<IExtensionPipelineContributor> ContributorsAddedInStage { get; }

    /// <summary>
    /// Gets the extension contributors that no earlier stage passed to the extensions: <see cref="Contributors"/> in the first high-level stage,
    /// and <see cref="ContributorsAddedInStage"/> in the later stages.
    /// </summary>
    public IReadOnlyCollection<IExtensionPipelineContributor> NewContributors => this.HighLevelStageIndex == 0 ? this.Contributors : this.ContributorsAddedInStage;

    /// <summary>
    /// Gets the compilation at the start of the stage.
    /// </summary>
    public CompilationModel StageInitialCompilation { get; }

    /// <summary>
    /// Gets the compilation that results from all aspects of the stage.
    /// </summary>
    public CompilationModel StageFinalCompilation { get; }

    /// <summary>
    /// Gets the zero-based index of the stage among the high-level stages of the pipeline. A low-level weaver starts a new high-level stage.
    /// </summary>
    public int HighLevelStageIndex { get; }
}
