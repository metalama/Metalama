// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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