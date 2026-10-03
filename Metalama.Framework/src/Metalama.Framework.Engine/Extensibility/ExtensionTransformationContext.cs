// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Extensibility.Transformations;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// Exposes the inputs of <see cref="PipelineExtension.ExecuteTransformingContributorsAsync"/>.
/// </summary>
/// <remarks>
/// One instance is shared by all extensions of a stage. It references the compilations of the stage, so it must not be stored in an object
/// that outlives the call.
/// </remarks>
[PublicAPI]
public sealed class ExtensionTransformationContext
{
    private readonly object _sync = new();
    private readonly IReadOnlyList<OrderedAspectLayer> _aspectLayers;
    private ExtensionTransformationFactory? _transformationFactory;

    internal ExtensionTransformationContext(
        AspectPipelineConfiguration pipelineConfiguration,
        IReadOnlyCollection<IExtensionPipelineContributor> contributors,
        IReadOnlyCollection<IExtensionPipelineContributor> contributorsAddedInStage,
        CompilationModel sourceCompilation,
        CompilationModel stageInitialCompilation,
        CompilationModel stageFinalCompilation,
        int highLevelStageIndex,
        UserDiagnosticSink diagnostics,
        SourceReferenceIndexStage sourceReferenceIndex,
        IReadOnlyList<OrderedAspectLayer> aspectLayers )
    {
        this._aspectLayers = aspectLayers;
        this.SourceReferenceIndex = sourceReferenceIndex;
        this.PipelineConfiguration = pipelineConfiguration;
        this.Contributors = contributors;
        this.ContributorsAddedInStage = contributorsAddedInStage;
        this.SourceCompilation = sourceCompilation;
        this.StageInitialCompilation = stageInitialCompilation;
        this.StageFinalCompilation = stageFinalCompilation;
        this.HighLevelStageIndex = highLevelStageIndex;
        this.Diagnostics = diagnostics;
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
    /// Gets the execution scenario: compile time, preview, live template or introspection.
    /// </summary>
    [Memo]
    public ExecutionScenario ExecutionScenario => this.ServiceProvider.GetRequiredService<ExecutionScenario>();

    /// <summary>
    /// Gets all extension contributors of the stage: those replayed from the contributor sources of the pipeline, such as fabrics and
    /// referenced assemblies, and those added by the aspects of the stage. The order is not deterministic.
    /// </summary>
    public IReadOnlyCollection<IExtensionPipelineContributor> Contributors { get; }

    /// <summary>
    /// Gets the extension contributors added by the aspects that executed in this stage. The order is not deterministic.
    /// </summary>
    public IReadOnlyCollection<IExtensionPipelineContributor> ContributorsAddedInStage { get; }

    /// <summary>
    /// Gets the model of the source compilation of the pipeline, which is the input of the first high-level stage.
    /// </summary>
    public CompilationModel SourceCompilation { get; }

    /// <summary>
    /// Gets the model from which the aspects of this stage started.
    /// </summary>
    public CompilationModel StageInitialCompilation { get; }

    /// <summary>
    /// Gets the model that results from all aspects of this stage.
    /// </summary>
    public CompilationModel StageFinalCompilation { get; }

    /// <summary>
    /// Gets <see cref="SourceCompilation"/> bound to the aspect repository of <see cref="StageFinalCompilation"/>.
    /// </summary>
    /// <remarks>
    /// The property is available only when <see cref="IsSourceStage"/> is <c>true</c>. In a later stage, the aspect repository contains the
    /// declarations of the compilation that a low-level weaver produced, and these declarations are not declarations of the source compilation.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The stage is not the source stage.</exception>
    public CompilationModel SourceCompilationWithFinalAspects
        => this.IsSourceStage
            ? this.SourceCompilationWithFinalAspectsCore
            : throw new InvalidOperationException(
                $"The {nameof(this.SourceCompilationWithFinalAspects)} property is available only in the source stage. Check the {nameof(this.IsSourceStage)} property." );

    /// <summary>
    /// Gets the value of <see cref="SourceCompilationWithFinalAspects"/> without the check of the stage.
    /// </summary>
    [Memo]
    private CompilationModel SourceCompilationWithFinalAspectsCore
        => this.SourceCompilation.WithAspectRepository( this.StageFinalCompilation.AspectRepository, "Source with final aspects" );

    /// <summary>
    /// Gets the zero-based index of the stage among the high-level stages that this pipeline executes.
    /// </summary>
    public int HighLevelStageIndex { get; }

    /// <summary>
    /// Gets a value indicating whether the aspects of this stage started from the source compilation. The linker of such a stage receives the
    /// syntax trees of the source compilation. Only the first high-level stage has this property.
    /// </summary>
    /// <remarks>
    /// The value compares the partial compilations and not the models, because all code-model versions of a stage share one partial compilation.
    /// </remarks>
    public bool IsSourceStage => ReferenceEquals( this.StageInitialCompilation.PartialCompilation, this.SourceCompilation.PartialCompilation );

    /// <summary>
    /// Gets the sink for the diagnostics and suppressions of the extensions.
    /// </summary>
    public UserDiagnosticSink Diagnostics { get; }

    /// <summary>
    /// Gets the factory of the transformations of this stage. It is shared by all extensions of the stage.
    /// </summary>
    public ExtensionTransformationFactory TransformationFactory
    {
        get
        {
            lock ( this._sync )
            {
                return this._transformationFactory ??= new ExtensionTransformationFactory(
                    this.StageFinalCompilation,
                    this._aspectLayers,
                    this.ServiceProvider.GetRequiredService<SyntaxGenerationOptions>() );
            }
        }
    }

    /// <summary>
    /// Completes the factory of transformations, if an extension used it, and returns the input of the linker.
    /// </summary>
    internal ExtensionLinkerInput CompleteTransformationFactory()
    {
        lock ( this._sync )
        {
            return this._transformationFactory?.Complete() ?? ExtensionLinkerInput.Empty;
        }
    }

    /// <summary>
    /// Gets the index of the references of the source compilation for this stage, which is shared by all extensions and built from the
    /// requirements that they returned from <see cref="PipelineExtension.GetSourceIndexRequirements"/>.
    /// </summary>
    public SourceReferenceIndexStage SourceReferenceIndex { get; }
}
