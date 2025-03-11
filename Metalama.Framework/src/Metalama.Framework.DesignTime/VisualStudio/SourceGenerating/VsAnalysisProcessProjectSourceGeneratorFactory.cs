// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using System.Collections.Concurrent;

namespace Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;

internal sealed class VsAnalysisProcessProjectSourceGeneratorFactory : IGlobalService
{
    private readonly GlobalServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<ProjectKey, Lazy<VsAnalysisProcessProjectSourceGenerator>> _handlers = new();

    public VsAnalysisProcessProjectSourceGeneratorFactory( GlobalServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
    }

    public VsAnalysisProcessProjectSourceGenerator GetOrCreateProjectHandler( IProjectOptions projectOptions, ProjectKey projectKey )
        => this._handlers.GetOrAdd(
                projectKey,
                new Lazy<VsAnalysisProcessProjectSourceGenerator>(
                    () => new VsAnalysisProcessProjectSourceGenerator( this._serviceProvider, projectOptions, projectKey ) ) )
            .Value;
}