// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.AspectExplorer;

internal sealed partial class AspectExplorerRpcService : RpcService<IAspectExplorerRpcApi>
{
    private readonly AnalysisProcessEventHub _eventHub;
    private readonly DesignTime.AspectExplorer.AspectDatabase _aspectDatabase;

    public AspectExplorerRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint serverEndpoint ) : base( serverEndpoint )
    {
        this._aspectDatabase = serviceProvider.GetRequiredService<DesignTime.AspectExplorer.AspectDatabase>();
        this._eventHub = serviceProvider.GetRequiredService<AnalysisProcessEventHub>();
    }

    public override void Dispose()
    {
        base.Dispose();

        this._eventHub.AspectClassesChanged -= this.OnAspectClassesChanged;
        this._eventHub.AspectInstancesChanged -= this.OnAspectInstancesChanged;
    }

    protected override IAspectExplorerRpcApi CreateApi( IRpcEventSender eventSender ) => new Api( this );

    private void OnAspectClassesChanged( ProjectKey projectKey )
    {
        this.RaiseEvent( new AspectClassesChangedEventData( projectKey ) );
    }

    private void OnAspectInstancesChanged( ProjectKey projectKey )
    {
        this.RaiseEvent( new AspectInstancesChangedEventData( projectKey ) );
    }
}