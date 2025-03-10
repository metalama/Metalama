// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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