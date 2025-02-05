// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus;

internal partial class CompileTimeCodeEditingStatusRpcService
{
    private sealed class Api : ICompileTimeCodeEditingStatusRpcApi
    {
        private readonly CompileTimeCodeEditingStatusRpcService _parent;

        public Api( CompileTimeCodeEditingStatusRpcService parent )
        {
            this._parent = parent;
        }

        public Task RegisterCallbackAsync( ProjectKey projectKey, CancellationToken cancellationToken )
        {
            this._parent.Logger.Trace?.Log( $"The client '{projectKey}' has connected. Registering the callback communication." );

            return Task.CompletedTask;
        }

        public Task OnCompileTimeCodeEditingCompletedAsync( CancellationToken cancellationToken = default )
        {
            this._parent._eventHub.OnCompileTimeCodeCompletedEditing();

            return Task.CompletedTask;
        }

        public Task OnUserInterfaceAttachedAsync( CancellationToken cancellationToken = default )
        {
            this._parent._eventHub.OnUserInterfaceAttached();

            return Task.CompletedTask;
        }
    }
}