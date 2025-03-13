// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.SourceGeneration;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Project;
using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;

[UsedImplicitly]
[PublicAPI]
public sealed class VsAnalysisProcessSourceGenerator : AnalysisProcessSourceGenerator
{
    private readonly VsAnalysisProcessProjectSourceGeneratorFactory _projectSourceGeneratorFactory;

    public VsAnalysisProcessSourceGenerator() : this( DesignTimeServiceProviderFactory.GetSharedServiceProvider() ) { }

    public VsAnalysisProcessSourceGenerator( ServiceProvider<IGlobalService> serviceProvider ) : base( serviceProvider )
    {
        this._projectSourceGeneratorFactory = serviceProvider.GetRequiredService<VsAnalysisProcessProjectSourceGeneratorFactory>();
    }

    private protected override ProjectSourceGenerator CreateSourceGeneratorImpl( IProjectOptions projectOptions, ProjectKey projectKey )
        => this._projectSourceGeneratorFactory.GetOrCreateProjectHandler( projectOptions, projectKey );
}