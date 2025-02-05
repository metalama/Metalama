// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Newtonsoft.Json;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;

[JsonObject]
internal sealed class RpcServiceInfo
{
    public string PipeName { get; }

    public string FactoryTypeName { get; }

    public RpcServiceInfo( string pipeName, string factoryTypeName )
    {
        this.FactoryTypeName = factoryTypeName;
        this.PipeName = pipeName;
    }
}