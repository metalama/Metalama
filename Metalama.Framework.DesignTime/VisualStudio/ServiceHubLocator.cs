// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.ServiceHub;
using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;

namespace Metalama.Framework.DesignTime.VisualStudio;

internal sealed class ServiceHubLocator : IServiceHubLocator, IServiceHubInfo
{
    public ServiceHubLocator( GlobalServiceProvider serviceProvider )
    {
        var hub = serviceProvider.GetRequiredService<IServiceHubRpcServiceProvider>().ServiceHub;
        this.PipeName = hub.PipeName;
        this.Version = EngineAssemblyMetadataReader.Instance.AssemblyVersion;
    }

    IServiceHubInfo IServiceHubLocator.ServiceHubInfo => this;

    public string PipeName { get; }

    public Version Version { get; }
}