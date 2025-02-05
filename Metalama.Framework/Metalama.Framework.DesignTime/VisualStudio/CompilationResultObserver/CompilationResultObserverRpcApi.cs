// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Rpc.Notifications;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.CompilationResultObserver;

internal sealed class CompilationResultObserverRpcApi : RpcService<ICompilationObserverRpcApi>
{
    private readonly AnalysisProcessEventHub _eventHub;

    public CompilationResultObserverRpcApi( GlobalServiceProvider serviceProvider, ServerEndpoint endpoint ) : base( endpoint )
    {
        this._eventHub = serviceProvider.GetRequiredService<AnalysisProcessEventHub>();
        this._eventHub.CompilationResultChangedEvent.RegisterHandler( this.OnCompilationResultChanged );
    }

    private void OnCompilationResultChanged( CompilationResultChangedEventData data )
    {
        this.RaiseEvent( data );
    }

    public override void Dispose()
    {
        base.Dispose();
        this._eventHub.CompilationResultChangedEvent.UnregisterHandler( this.OnCompilationResultChanged );
    }

    protected override ICompilationObserverRpcApi CreateApi( IRpcEventSender eventSender ) => new Api();

    private sealed class Api : ICompilationObserverRpcApi;
}