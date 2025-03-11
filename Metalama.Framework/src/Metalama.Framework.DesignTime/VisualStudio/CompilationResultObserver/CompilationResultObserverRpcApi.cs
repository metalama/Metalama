// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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