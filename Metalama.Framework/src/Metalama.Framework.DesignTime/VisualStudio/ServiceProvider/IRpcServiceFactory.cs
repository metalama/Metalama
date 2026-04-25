// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;

public interface IRpcServiceFactory
{
    /// <summary>
    /// Gets the name of the extension that implements the service, or <c>null</c> if the service is implemented
    /// by the core. The extension name is the value of <c>IDesignTimeExtension.Name</c>.
    /// </summary>

    string? ExtensionName { get; }

    RpcService CreateRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint endpoint );

    RpcClient CreateRpcClient( GlobalServiceProvider serviceProvider, ClientEndpoint endpoint );
}