// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus;

/// <summary>
/// This component runs in the user process. It manages the status of the compile-time status bar.
/// </summary>
internal sealed class CompileTimeCodeEditingStatusRpcClient : RpcClient<ICompileTimeCodeEditingStatusRpcApi>
{
    private ImmutableDictionary<ProjectKey, ImmutableArray<IDiagnosticData>> _compileTimeErrorsPerProject =
        ImmutableDictionary<ProjectKey, ImmutableArray<IDiagnosticData>>.Empty;

    public CompileTimeCodeEditingStatusRpcClient( ClientEndpoint endpoint ) : base( endpoint ) { }

    // ReSharper disable once UnusedParameter.Global
    public Task<ImmutableDictionary<ProjectKey, ImmutableArray<IDiagnosticData>>> GetCompileTimeErrorsAsync( CancellationToken cancellationToken )
        => Task.FromResult( this._compileTimeErrorsPerProject );

    protected override Task OnEventReceivedAsync( RpcEventData eventData, CancellationToken cancellationToken )
    {
        if ( eventData is CompileTimeErrorsChangedEventData errors )
        {
            while ( true )
            {
                var oldDictionary = this._compileTimeErrorsPerProject;
                var dictionary = oldDictionary.SetItem( errors.ProjectKey, errors.Errors.ToImmutableArray<IDiagnosticData>() );

                if ( Interlocked.CompareExchange( ref this._compileTimeErrorsPerProject, dictionary, oldDictionary ) == oldDictionary )
                {
                    break;
                }
            }
        }

        return base.OnEventReceivedAsync( eventData, cancellationToken );
    }
}