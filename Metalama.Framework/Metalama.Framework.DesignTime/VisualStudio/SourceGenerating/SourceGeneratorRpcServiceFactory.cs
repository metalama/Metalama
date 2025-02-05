// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;

internal sealed class SourceGeneratorRpcServiceFactory : IRpcServiceFactory
{
    public RpcService CreateRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint endpoint )
        => new SourceGeneratorRpcService( serviceProvider, endpoint );

    public RpcClient CreateRpcClient( GlobalServiceProvider serviceProvider, ClientEndpoint endpoint )
        => new SourceGeneratorRpcClient( serviceProvider, endpoint );
}