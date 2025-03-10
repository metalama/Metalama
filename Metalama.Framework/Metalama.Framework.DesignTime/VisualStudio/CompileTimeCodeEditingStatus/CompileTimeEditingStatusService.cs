// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.Diagnostics;
using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Services;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus;

/// <summary>
/// User-side implementation of the <see cref="ICompileTimeEditingStatusService"/> interface.
/// It essentially forwards messages to and from the analysis process.
/// </summary>
internal sealed class CompileTimeEditingStatusService : ICompileTimeEditingStatusService, ICompileTimeErrorStatusService, IDisposable
{
    private readonly ServiceHubRpcService _serviceHub;
    private readonly TaskBag _pendingTasks;
    private bool _userInterfaceAttached;

    // We use an immutable dictionary to have a simple consistent enumerator.
    private volatile ImmutableDictionary<ProjectKey, ImmutableArray<IDiagnosticData>> _compileTimeErrorsByProject =
        ImmutableDictionary<ProjectKey, ImmutableArray<IDiagnosticData>>.Empty;

    public CompileTimeEditingStatusService( GlobalServiceProvider serviceProvider )
    {
        var logger = serviceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
        this._pendingTasks = new TaskBag( logger, serviceProvider );
        this._serviceHub = serviceProvider.GetRequiredService<IServiceHubRpcServiceProvider>().ServiceHub;
        this._serviceHub.EventReceived += this.OnEventReceived;
        this._serviceHub.EndpointAdded += this.OnEndpointAdded;
    }

    private void OnEventReceived( RpcEventData eventData )
    {
        if ( eventData.Category != nameof(ICompileTimeCodeEditingStatusRpcApi) )
        {
            return;
        }

        switch ( eventData )
        {
            case CompileTimeErrorsChangedEventData errorsChanged:
                this.OnProjectErrorsReceived( errorsChanged );

                break;

            case CompileTimeEditingStatusChangedEventData statusChanged:
                this.IsEditing = statusChanged.IsEditing;
                this.IsEditingChanged?.Invoke( this.IsEditing );

                break;
        }
    }

    private void OnProjectErrorsReceived( CompileTimeErrorsChangedEventData errorsChanged )
    {
        var spinWait = default(SpinWait);

        while ( true )
        {
            var localCopy = this._compileTimeErrorsByProject;
            var modified = localCopy.SetItem( errorsChanged.ProjectKey, errorsChanged.Errors.ToImmutableArray<IDiagnosticData>() );

            if ( Interlocked.CompareExchange( ref this._compileTimeErrorsByProject, modified, localCopy ) == localCopy )
            {
                this.CompileTimeErrors = this._compileTimeErrorsByProject.SelectMany( x => x.Value ).ToArray();
                this.CompileTimeErrorsChanged?.Invoke();

                return;
            }

            spinWait.SpinOnce();
        }
    }

    private void SetCompileTimeErrorsForProjects( ImmutableDictionary<ProjectKey, ImmutableArray<IDiagnosticData>> errors )
    {
        var spinWait = default(SpinWait);

        while ( true )
        {
            var localCopy = this._compileTimeErrorsByProject;
            var modified = localCopy;

            foreach ( var group in errors )
            {
                modified = modified.SetItem( group.Key, group.Value );
            }

            if ( Interlocked.CompareExchange( ref this._compileTimeErrorsByProject, modified, localCopy ) == localCopy )
            {
                this.CompileTimeErrors = this._compileTimeErrorsByProject.SelectMany( x => x.Value ).ToArray();
                this.CompileTimeErrorsChanged?.Invoke();

                return;
            }

            spinWait.SpinOnce();
        }
    }

    public bool IsEditing { get; private set; }

    public event Action<bool>? IsEditingChanged;

    public IDiagnosticData[] CompileTimeErrors { get; private set; } = [];

    public event Action? CompileTimeErrorsChanged;

    private void OnEndpointAdded( RpcServiceProviderClientEndpoint endpoint )
    {
        this._pendingTasks.Run(
            async () =>
            {
                var client = await endpoint.GetOrWaitForClientAsync<CompileTimeCodeEditingStatusRpcClient>( default );

                var compileTimeErrors = await client.GetCompileTimeErrorsAsync( CancellationToken.None );
                this.SetCompileTimeErrorsForProjects( compileTimeErrors );

                if ( this._userInterfaceAttached )
                {
                    var api = await client.GetApiAsync();
                    await api.OnUserInterfaceAttachedAsync();
                }
            } );
    }

    public async Task OnEditingCompletedAsync( CancellationToken cancellationToken )
    {
        foreach ( var endpoint in this._serviceHub.Endpoints )
        {
            var client = endpoint.GetClient<CompileTimeCodeEditingStatusRpcClient>();

            if ( client == null )
            {
                continue;
            }

            var api = await client.GetApiAsync( cancellationToken );
            await api.OnCompileTimeCodeEditingCompletedAsync( cancellationToken );
        }
    }

    public async Task OnUserInterfaceAttachedAsync( CancellationToken cancellationToken )
    {
        this._userInterfaceAttached = true;

        foreach ( var endpoint in this._serviceHub.Endpoints )
        {
            var client = endpoint.GetClient<CompileTimeCodeEditingStatusRpcClient>();

            if ( client == null )
            {
                continue;
            }

            var api = await client.GetApiAsync( cancellationToken );
            await api.OnUserInterfaceAttachedAsync( cancellationToken );
        }
    }

    public void Dispose()
    {
        this._serviceHub.EndpointAdded -= this.OnEndpointAdded;
        this._serviceHub.EventReceived -= this.OnEventReceived;
    }
}