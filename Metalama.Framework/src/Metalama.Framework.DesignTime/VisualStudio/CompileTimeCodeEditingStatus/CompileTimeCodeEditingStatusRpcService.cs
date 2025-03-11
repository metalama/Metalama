// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Services;
using System.Collections.Immutable;

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

    private void OnCompileTimeErrorsChanged( ProjectKey projectKey, ImmutableArray<DiagnosticData> errors )
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