// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus;

internal sealed partial class CompileTimeCodeEditingStatusRpcService : RpcService<ICompileTimeCodeEditingStatusRpcApi>
{
    private readonly AnalysisProcessEventHub _eventHub;

    public CompileTimeCodeEditingStatusRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint serverEndpoint ) : base( serverEndpoint )
    {
        this._eventHub = serviceProvider.GetRequiredService<AnalysisProcessEventHub>();

        this._eventHub.IsEditingCompileTimeCodeChanged += this.OnIsEditingCompileTimeCodeChanged;
        this._eventHub.CompileTimeErrorsChanged += this.OnCompileTimeErrorsChanged;
    }

    private void OnIsEditingCompileTimeCodeChanged( bool isEditing )
    {
        this.RaiseEvent( new CompileTimeEditingStatusChangedEventData( isEditing ) );
    }

    private void OnCompileTimeErrorsChanged( ProjectKey projectKey, IReadOnlyCollection<DiagnosticData> errors )
    {
        this.RaiseEvent( new CompileTimeErrorsChangedEventData( projectKey, errors ) );
    }

    public override void Dispose()
    {
        base.Dispose();

        this._eventHub.IsEditingCompileTimeCodeChanged -= this.OnIsEditingCompileTimeCodeChanged;
        this._eventHub.CompileTimeErrorsChanged -= this.OnCompileTimeErrorsChanged;
    }

    protected override ICompileTimeCodeEditingStatusRpcApi CreateApi( IRpcEventSender eventSender ) => new Api( this );
}