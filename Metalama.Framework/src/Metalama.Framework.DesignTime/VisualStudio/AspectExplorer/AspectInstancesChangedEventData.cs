// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Newtonsoft.Json;

namespace Metalama.Framework.DesignTime.VisualStudio.AspectExplorer;

[JsonObject]
internal sealed class AspectInstancesChangedEventData : RpcEventData
{
    public override string Category => nameof(IAspectExplorerRpcApi);

    public ProjectKey ProjectKey { get; }

    public AspectInstancesChangedEventData( ProjectKey projectKey )
    {
        this.ProjectKey = projectKey;
    }
}