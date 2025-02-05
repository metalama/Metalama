// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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