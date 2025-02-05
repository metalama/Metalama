// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.Preview;

internal sealed partial class PreviewTransformationRpcService : RpcService<IPreviewTransformationRpcApi>
{
    private readonly ITransformationPreviewServiceImpl _transformationPreviewService;

    public PreviewTransformationRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint serverEndpoint ) : base( serverEndpoint )
    {
        this._transformationPreviewService = serviceProvider.GetRequiredService<ITransformationPreviewServiceImpl>();
    }

    protected override IPreviewTransformationRpcApi CreateApi( IRpcEventSender eventSender ) => new Api( this );
}