// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// Stores the "static" configuration of the pipeline, i.e. the things that don't change
/// when the user code changes. This includes the <see cref="CompileTimeProject"/>, the pipeline stages and
/// the order of layers.
/// </summary>
public sealed class AspectPipelineConfiguration
{
    public CompileTimeDomain Domain { get; }

    internal ImmutableArray<PipelineStageConfiguration> Stages { get; }

    public AspectClass GetAspectClass( Type aspectType )
        => (AspectClass)
            this.BoundAspectClasses.Single<IBoundAspectClass>( c => c.Type == aspectType );

    internal AspectClassCollection BoundAspectClasses { get; }

    public IReadOnlyCollection<IAspectClass> AspectClasses => this.BoundAspectClasses;

    public ulong AspectClassesHashCode => this.BoundAspectClasses.HashCode;

    internal ImmutableArray<OrderedAspectLayer> AspectLayers { get; }

    internal CompileTimeProject? CompileTimeProject { get; }

    /// <summary>
    /// Gets the complete <see cref="DiagnosticManifest"/>, which includes user diagnostics (from the <see cref="CompileTimeProject"/>)
    /// and extension diagnostics.
    /// </summary>
    public DiagnosticManifest DiagnosticManifest { get; }

    private CompileTimeProjectRepository CompileTimeProjectRepository { get; }

    internal PipelineContributorSources? FabricsContributors { get; }

    public ImmutableArray<string> FabricTypeNames { get; }

    public ProjectModel ProjectModel { get; }

    public ProjectServiceProvider ServiceProvider { get; }

    public ImmutableArray<PipelineExtension> Extensions { get; }

    internal AspectPipelineConfiguration(
        CompileTimeDomain domain,
        ImmutableArray<PipelineStageConfiguration> stages,
        AspectClassCollection aspectClasses,
        ImmutableArray<OrderedAspectLayer> aspectLayers,
        CompileTimeProject? compileTimeProject,
        CompileTimeProjectRepository compileTimeProjectRepository,
        PipelineContributorSources? fabricContributors,
        ImmutableArray<string> fabricTypeNames,
        ProjectModel projectModel,
        ProjectServiceProvider serviceProvider,
        ImmutableArray<PipelineExtension> extensions,
        DiagnosticManifest diagnosticManifest )
    {
        this.Domain = domain;
        this.Stages = stages;
        this.BoundAspectClasses = aspectClasses;
        this.AspectLayers = aspectLayers;
        this.CompileTimeProject = compileTimeProject;
        this.CompileTimeProjectRepository = compileTimeProjectRepository;
        this.FabricsContributors = fabricContributors;
        this.FabricTypeNames = fabricTypeNames;
        this.ProjectModel = projectModel;
        this.ServiceProvider = serviceProvider;
        this.Extensions = extensions;
        this.DiagnosticManifest = diagnosticManifest;
    }

    public AspectPipelineConfiguration WithServiceProvider( in ProjectServiceProvider serviceProvider )
        => new(
            this.Domain,
            this.Stages,
            this.BoundAspectClasses,
            this.AspectLayers,
            this.CompileTimeProject,
            this.CompileTimeProjectRepository,
            this.FabricsContributors,
            this.FabricTypeNames,
            this.ProjectModel,
            serviceProvider,
            this.Extensions,
            this.DiagnosticManifest );

    internal UserCodeInvoker UserCodeInvoker => this.ServiceProvider.GetRequiredService<UserCodeInvoker>();
}