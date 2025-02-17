// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Framework.DesignTime.Rpc;

public abstract partial class ClientEndpoint
{
    private sealed class NullCallback( ClientEndpoint parent ) : IRpcCallback
    {
        private readonly ClientEndpoint _parent = parent;

        public Task RaiseEventAsync( RpcEventEnvelope envelope, [UsedImplicitly] CancellationToken cancellationToken )
        {
            this._parent.Logger.Trace?.Log( $"NullCallback.RaiseEventAsync: Received event {envelope.Data.Category} from {envelope.OriginatingApi}." );

            return Task.CompletedTask;
        }
    }
}