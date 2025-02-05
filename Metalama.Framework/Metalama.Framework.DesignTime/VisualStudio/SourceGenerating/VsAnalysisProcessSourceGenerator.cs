// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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