// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// Represents something that extends the Metalama pipeline.
/// </summary>
[Durable]
[PublicAPI]
public abstract class PipelineExtension
{
    public virtual bool Initialize( PipelineExtensionInitializationContext context ) => true;

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

    public virtual IEnumerable<IPipelineContributor> GetPipelineContributorsFromTransitiveManifest(
        ImmutableArray<ITransitiveAspectsManifestExtension> extensions,
        IAspectClassResolver aspectClassResolver,
        UserDiagnosticSink diagnosticSink )
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
    /// Returns the requirements of the extension for the index of the references of the source compilation. The method is invoked once per
    /// pipeline execution, on every extension, after <see cref="ExecutePipelineContributorsAsync"/> and before
    /// <see cref="ExecuteTransformingContributorsAsync"/>.
    /// </summary>
    /// <remarks>
    /// The requirements of all extensions are merged, and the index is built at most once, when an extension first reads it through
    /// <see cref="ExtensionTransformationContext.SourceReferenceIndex"/>. An extension that returns no requirement can still read the index, but
    /// the index then contains only the references that other extensions requested. A contribution made by an aspect that executes after a low-level aspect weaver is not processed.
    /// </remarks>
    public virtual SourceIndexRequirements GetSourceIndexRequirements( SourceIndexRequirementsContext context ) => SourceIndexRequirements.None;

    /// <summary>
    /// Executes the contributors that produce code transformations. The method is invoked once per pipeline execution, on the source
    /// compilation, before any low-level aspect weaver, after <see cref="ExecutePipelineContributorsAsync"/> has been invoked for all extensions
    /// and before the linker runs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method is invoked at compile time and in the preview, live-template and introspection scenarios. It is not invoked at design time
    /// or in the WPF precompilation scenario, because these scenarios run no linker.
    /// </para>
    /// <para>
    /// A contribution made by an aspect that executes after a low-level aspect weaver is not processed.
    /// </para>
    /// </remarks>
    public virtual Task ExecuteTransformingContributorsAsync( ExtensionTransformationContext context, CancellationToken cancellationToken )
        => Task.CompletedTask;

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