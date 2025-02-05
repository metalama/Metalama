// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using Newtonsoft.Json;

namespace Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus;

[JsonObject]
internal sealed class CompileTimeEditingStatusChangedEventData : RpcEventData
{
    public override string Category => nameof(ICompileTimeCodeEditingStatusRpcApi);

    public bool IsEditing { get; }

    public CompileTimeEditingStatusChangedEventData( bool isEditing )
    {
        this.IsEditing = isEditing;
    }
}