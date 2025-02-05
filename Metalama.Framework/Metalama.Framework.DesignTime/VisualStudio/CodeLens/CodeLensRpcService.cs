// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.CodeLens;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.CodeLens;

internal sealed partial class CodeLensRpcService : RpcService<ICodeLensRpcApi>
{
    private readonly ICodeLensServiceImpl _codeLensServiceImpl;

    public CodeLensRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint serverEndpoint ) : base( serverEndpoint )
    {
        this._codeLensServiceImpl = serviceProvider.GetRequiredService<ICodeLensServiceImpl>();
    }

    protected override ICodeLensRpcApi CreateApi( IRpcEventSender eventSender ) => new Api( this );
}