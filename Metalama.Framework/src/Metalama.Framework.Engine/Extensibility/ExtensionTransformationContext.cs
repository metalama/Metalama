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
/// The transforming hook runs once per pipeline execution, on the source compilation, before any low-level aspect weaver. One instance is shared
/// by all extensions. It references the compilations of the pipeline execution, so it must not be stored in an object that outlives the call.
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
        CompilationModel finalCompilation,
        UserDiagnosticSink diagnostics,
        SourceReferenceIndex sourceReferenceIndex,
        IReadOnlyList<OrderedAspectLayer> aspectLayers )
    {
        this._aspectLayers = aspectLayers;
        this.SourceReferenceIndex = sourceReferenceIndex;
        this.PipelineConfiguration = pipelineConfiguration;
        this.Contributors = contributors;
        this.SourceCompilation = sourceCompilation;
        this.FinalCompilation = finalCompilation;
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
    /// Gets all extension contributors: those of the contributor sources of the pipeline, such as fabrics and referenced assemblies, and those
    /// added by the aspects. The order is not deterministic.
    /// </summary>
    /// <remarks>
    /// A contributor added by an aspect that executes after a low-level aspect weaver is not in this collection.
    /// </remarks>
    public IReadOnlyCollection<IExtensionPipelineContributor> Contributors { get; }

    /// <summary>
    /// Gets the model of the source compilation of the pipeline, from which the aspects started.
    /// </summary>
    public CompilationModel SourceCompilation { get; }

    /// <summary>
    /// Gets the model that results from all aspects that execute before any low-level aspect weaver.
    /// </summary>
    public CompilationModel FinalCompilation { get; }

    /// <summary>
    /// Gets <see cref="SourceCompilation"/> bound to the aspect repository of <see cref="FinalCompilation"/>, so that the declarations of
    /// the source compilation report the aspects.
    /// </summary>
    [Memo]
    public CompilationModel SourceCompilationWithFinalAspects
        => this.SourceCompilation.WithAspectRepository( this.FinalCompilation.AspectRepository, "Source with final aspects" );

    /// <summary>
    /// Gets the sink for the diagnostics and suppressions of the extensions.
    /// </summary>
    public UserDiagnosticSink Diagnostics { get; }

    /// <summary>
    /// Gets the factory of the transformations of the pipeline execution. It is shared by all extensions.
    /// </summary>
    public ExtensionTransformationFactory TransformationFactory
    {
        get
        {
            lock ( this._sync )
            {
                return this._transformationFactory ??= new ExtensionTransformationFactory(
                    this.FinalCompilation,
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
    public SourceReferenceIndex SourceReferenceIndex { get; }
}
