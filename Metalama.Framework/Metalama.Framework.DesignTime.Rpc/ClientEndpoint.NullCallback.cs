// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Framework.DesignTime.Rpc;

public abstract partial class ClientEndpoint
{
    private sealed class NullCallback : IRpcCallback
    {
        public static NullCallback Instance { get; } = new();

        private NullCallback() { }

        public Task RaiseEventAsync( RpcEventEnvelope envelope, [UsedImplicitly] CancellationToken cancellationToken ) => Task.CompletedTask;
    }
}