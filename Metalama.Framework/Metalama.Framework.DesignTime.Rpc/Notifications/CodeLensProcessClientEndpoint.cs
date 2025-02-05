// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Framework.DesignTime.Rpc.Notifications;

[PublicAPI]
public sealed class CodeLensProcessClientEndpoint : ClientEndpoint
{
    public CodeLensProcessClientEndpoint( IServiceProvider serviceProvider, string pipeName ) : base(
        serviceProvider,
        pipeName )
    {
        this.Client = new CodeLensProcessRpcClient( this, serviceProvider );
    }

    public CodeLensProcessRpcClient Client { get; }

    protected override IEnumerable<RpcClient> CreateServiceClients() => [this.Client];
}