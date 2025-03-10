// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.Preview;

internal sealed class PreviewTransformationRpcServiceFactory : IRpcServiceFactory
{
    public RpcService CreateRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint endpoint )
        => new PreviewTransformationRpcService( serviceProvider, endpoint );

    public RpcClient CreateRpcClient( GlobalServiceProvider serviceProvider, ClientEndpoint endpoint ) => new PreviewTransformationRpcClient( endpoint );
}