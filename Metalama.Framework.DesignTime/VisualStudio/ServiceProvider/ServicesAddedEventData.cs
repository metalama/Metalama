// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using Newtonsoft.Json;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;

[JsonObject]
internal sealed class ServicesAddedEventData : RpcEventData
{
    public ServicesAddedEventData( ImmutableArray<RpcServiceInfo> services )
    {
        this.Services = services;
    }

    public ImmutableArray<RpcServiceInfo> Services { get; }

    public override string Category => nameof(IRpcServiceProviderApi);
}