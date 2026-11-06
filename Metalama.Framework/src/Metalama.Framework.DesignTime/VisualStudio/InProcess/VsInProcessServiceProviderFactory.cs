// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.VersionNeutral;
using Metalama.Framework.DesignTime.VisualStudio.Services;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.InProcess;

/// <summary>
/// The service provider factory of a process that hosts both processes of the Visual Studio design-time architecture. It is
/// used only when <see cref="DesignTimeHostOverride.IsVisualStudioInProcess"/> is <c>true</c>.
/// </summary>
/// <remarks>
/// The factory builds the service provider of the Visual Studio user process, which is the one that is registered in
/// <see cref="Contracts.EntryPoint.DesignTimeEntryPointManager.Instance"/>. It adds <see cref="VsInProcessAnalysisProcess"/>,
/// which builds the service provider of the analysis process in the same process.
/// </remarks>
internal sealed class VsInProcessServiceProviderFactory : VsUserProcessServiceProviderFactory
{
    [UsedImplicitly]
    public VsInProcessServiceProviderFactory() : base( null ) { }

    protected override CompilerServiceProvider CreateCompilerServiceProvider() => new VsInProcessCompilerServiceProvider();

    protected override ServiceProvider<IGlobalService> AddServices( ServiceProvider<IGlobalService> serviceProvider )
    {
        serviceProvider = base.AddServices( serviceProvider ).WithService( sp => new VsInProcessAnalysisProcess( sp ) );

        // The service is created lazily, but the analysis process must exist as soon as the services are registered: a test
        // may call the AppDomain data slots that the constructor publishes before it asks for any service.
        _ = ((GlobalServiceProvider) serviceProvider).GetRequiredService<VsInProcessAnalysisProcess>();

        return serviceProvider;
    }
}
