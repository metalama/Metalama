// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
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

        serviceProvider = serviceProvider
            .WithService( sp => new TransformationPreviewServiceImpl( sp ) )
            .WithService( sp => new CodeLensServiceImpl( sp ) )
            .WithService( sp => new ServiceHubClientEndpointProvider( sp ) )
            .WithService( sp => new RpcServiceProviderServerEndpointProvider( sp ) )
            .WithService( sp => new VsAnalysisProcessProjectSourceGeneratorFactory( sp ) );

        return serviceProvider;
    }
}