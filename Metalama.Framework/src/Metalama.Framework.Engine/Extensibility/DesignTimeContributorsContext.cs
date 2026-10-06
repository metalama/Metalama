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
/// The hook runs once per pipeline execution, on the source compilation, before any low-level aspect weaver, as the transforming hook does at
/// compile time. Its transitive contributors are kept in the design-time result. A contributor added by an aspect that executes after a
/// low-level aspect weaver is not passed to the hook.
/// </para>
/// <para>
/// One instance is shared by all extensions. It references the compilations of the pipeline execution, so it must not be stored in an object
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
        CompilationModel sourceCompilation,
        CompilationModel finalCompilation )
    {
        this.PipelineConfiguration = pipelineConfiguration;
        this.Contributors = contributors;
        this.SourceCompilation = sourceCompilation;
        this.FinalCompilation = finalCompilation;
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
    /// Gets all extension contributors: those of the contributor sources of the pipeline, and those added by the aspects.
    /// </summary>
    public IReadOnlyCollection<IExtensionPipelineContributor> Contributors { get; }

    /// <summary>
    /// Gets the source compilation of the pipeline, from which the aspects started.
    /// </summary>
    public CompilationModel SourceCompilation { get; }

    /// <summary>
    /// Gets the compilation that results from all aspects that execute before any low-level aspect weaver.
    /// </summary>
    public CompilationModel FinalCompilation { get; }
}
