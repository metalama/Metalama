// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
            this._parent.Logger.Trace?.Log( $"Callback.RaiseEventAsync: Received event {envelope.Data.Category} from {envelope.OriginatingApi}." );

            return this._parent.OnEventReceivedAsync( envelope, cancellationToken );
        }
    }
}