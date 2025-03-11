// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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