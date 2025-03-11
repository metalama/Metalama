// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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