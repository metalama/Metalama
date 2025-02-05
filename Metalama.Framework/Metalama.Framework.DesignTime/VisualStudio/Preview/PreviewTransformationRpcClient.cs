// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.VisualStudio.Preview;

internal sealed class PreviewTransformationRpcClient : RpcClient<IPreviewTransformationRpcApi>
{
    public PreviewTransformationRpcClient( ClientEndpoint endpoint ) : base( endpoint ) { }
}