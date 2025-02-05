// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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