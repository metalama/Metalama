// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;

public interface IRpcServiceFactory
{
    RpcService CreateRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint endpoint );

    RpcClient CreateRpcClient( GlobalServiceProvider serviceProvider, ClientEndpoint endpoint );
}