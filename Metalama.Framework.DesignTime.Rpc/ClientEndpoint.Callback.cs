// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.DesignTime.Rpc;

public abstract partial class ClientEndpoint
{
    private sealed class Callback : IRpcCallback
    {
        private readonly ClientEndpoint _parent;

        public Callback( ClientEndpoint parent )
        {
            this._parent = parent;
        }

        public Task RaiseEventAsync( RpcEventEnvelope envelope, CancellationToken cancellationToken )
        {
            return this._parent.OnEventReceivedAsync( envelope, cancellationToken );
        }
    }
}