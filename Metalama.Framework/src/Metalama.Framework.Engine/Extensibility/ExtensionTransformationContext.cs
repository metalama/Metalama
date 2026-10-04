// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Extensibility.CallSites;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// Exposes the inputs of <see cref="PipelineExtension.ExecuteTransformingContributorsAsync"/>.
/// </summary>
/// <remarks>
/// The transforming hook runs only in the source stage, which is the first high-level stage of the pipeline execution, before any low-level
/// weaver. One instance is shared by all extensions. It references the compilations of the source stage, so it must not be stored in an object
/// that outlives the call.
/// </remarks>
[PublicAPI]
public sealed class ExtensionTransformationContext
{
    /// <summary>
    /// The lock that serializes the creation and the completion of <see cref="_transformationFactory"/>.
    /// </summary>
    private readonly object _sync = new();

    /// <summary>
    /// The ordered aspect layers of the pipeline, which are passed to the factory of transformations.
    /// </summary>
    private readonly IReadOnlyList<OrderedAspectLayer> _aspectLayers;

    /// <summary>
    /// The factory of transformations, created by the first access to <see cref="TransformationFactory"/>, or <c>null</c> when no extension
    /// has used it.
    /// </summary>
    private ExtensionTransformationFactory? _transformationFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionTransformationContext"/> class.
    /// </summary>
    internal ExtensionTransformationContext(
        AspectPipelineConfiguration pipelineConfiguration,
        IReadOnlyCollection<IExtensionPipelineContributor> contributors,
        CompilationModel sourceCompilation,
        CompilationModel stageFinalCompilation,
        UserDiagnosticSink diagnostics,
        SourceReferenceIndexStage sourceReferenceIndex,
        IReadOnlyList<OrderedAspectLayer> aspectLayers )
    {
        this._aspectLayers = aspectLayers;
        this.SourceReferenceIndex = sourceReferenceIndex;
        this.PipelineConfiguration = pipelineConfiguration;
        this.Contributors = contributors;
        this.SourceCompilation = sourceCompilation;
        this.StageFinalCompilation = stageFinalCompilation;
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
    /// Gets all extension contributors of the source stage: those of the contributor sources of the pipeline, such as fabrics and referenced
    /// assemblies, and those added by the aspects of the source stage. The order is not deterministic.
    /// </summary>
    /// <remarks>
    /// A contributor added by an aspect that executes after a low-level weaver is not in this collection, because the hook does not run in later
    /// stages. <see cref="ExtensionContributionOrigin.HighLevelStageIndex"/> lets an extension report such a contributor from
    /// <see cref="PipelineExtension.ExecutePipelineContributorsAsync"/>.
    /// </remarks>
    public IReadOnlyCollection<IExtensionPipelineContributor> Contributors { get; }

    /// <summary>
    /// Gets the model of the source compilation of the pipeline, from which the aspects of the source stage started.
    /// </summary>
    public CompilationModel SourceCompilation { get; }

    /// <summary>
    /// Gets the model that results from all aspects of the source stage.
    /// </summary>
    public CompilationModel StageFinalCompilation { get; }

    /// <summary>
    /// Gets <see cref="SourceCompilation"/> bound to the aspect repository of <see cref="StageFinalCompilation"/>, so that the declarations of
    /// the source compilation report the aspects of the source stage.
    /// </summary>
    [Memo]
    public CompilationModel SourceCompilationWithFinalAspects
        => this.SourceCompilation.WithAspectRepository( this.StageFinalCompilation.AspectRepository, "Source with final aspects" );

    /// <summary>
    /// Gets the sink for the diagnostics and suppressions of the extensions.
    /// </summary>
    public UserDiagnosticSink Diagnostics { get; }

    /// <summary>
    /// Gets the factory of the transformations of the source stage. It is shared by all extensions.
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
    /// Gets the index of the references of the source compilation, which is shared by all extensions and built from the requirements that they
    /// returned from <see cref="PipelineExtension.GetSourceIndexRequirements"/>.
    /// </summary>
    public SourceReferenceIndexStage SourceReferenceIndex { get; }
}
