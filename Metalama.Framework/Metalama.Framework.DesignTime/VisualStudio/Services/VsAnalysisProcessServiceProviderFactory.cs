// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.CodeLens;
using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.Services;

internal sealed class VsAnalysisProcessServiceProviderFactory : DesignTimeAnalysisProcessServiceProviderFactory
{
    public VsAnalysisProcessServiceProviderFactory() : this( null ) { }

    public VsAnalysisProcessServiceProviderFactory( DesignTimeEntryPointManager? entryPointManager ) : base(
        entryPointManager,
        DesignTimeProcessKind.VsAnalysisProcess ) { }

    protected override ServiceProvider<IGlobalService> AddServices( ServiceProvider<IGlobalService> serviceProvider )
    {
        serviceProvider = base.AddServices( serviceProvider );

        serviceProvider =
            serviceProvider.WithServices( new TransformationPreviewServiceImpl( serviceProvider ), new CodeLensServiceImpl( serviceProvider ) );

        if ( ServiceHubClientEndpoint.TryStart(
                serviceProvider,
                CancellationToken.None,
                out var serviceHubApiProvider ) )
        {
            serviceProvider = serviceProvider.WithService( serviceHubApiProvider );

            var analysisProcessEndpoint = RpcServiceProviderServerEndpoint.GetInstance( serviceProvider );
            serviceProvider = serviceProvider.WithService( analysisProcessEndpoint );
        }

        serviceProvider = serviceProvider.WithService( new VsAnalysisProcessProjectSourceGeneratorFactory( serviceProvider ) );

        return serviceProvider;
    }
}