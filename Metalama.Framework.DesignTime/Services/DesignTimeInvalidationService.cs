// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Rpc.Notifications;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using System.Linq.Expressions;

namespace Metalama.Framework.DesignTime.Services;

internal sealed class DesignTimeInvalidationService : IGlobalService
{
    private readonly WorkspaceProvider _workspaceProvider;

    public DesignTimeInvalidationService( GlobalServiceProvider serviceProvider )
    {
        this._workspaceProvider = serviceProvider.GetRequiredService<WorkspaceProvider>();

        var hub = serviceProvider.GetRequiredService<AnalysisProcessEventHub>();
        hub.CompilationResultChangedEvent.RegisterHandler( this.OnCompilationResultChanged );
    }

    private void OnCompilationResultChanged( CompilationResultChangedEventData data )
    {
        if ( !this._workspaceProvider.TryGetWorkspace( out var workspace ) )
        {
            // The workspace is not available yet.
            return;
        }

        _diagnosticsRefreshAction?.Invoke( workspace.Services.HostServices );
    }

    private static readonly Action<HostServices>? _diagnosticsRefreshAction = GetDiagnosticsRefreshAction();

    private static Action<HostServices>? GetDiagnosticsRefreshAction()
    {
        var codeAnalysisFeaturesAssembly = typeof(CompletionProvider).Assembly;
        var iDiagnosticsRefresher = codeAnalysisFeaturesAssembly.GetType( "Microsoft.CodeAnalysis.Diagnostics.IDiagnosticsRefresher" );
        var requestWorkspaceRefreshMethod = iDiagnosticsRefresher?.GetMethod( "RequestWorkspaceRefresh", Type.EmptyTypes );

        if ( iDiagnosticsRefresher == null || requestWorkspaceRefreshMethod == null )
        {
            return null;
        }

        var codeAnalysisWorkspacesAssembly = typeof(Workspace).Assembly;
        var iMefHostExportProvider = codeAnalysisWorkspacesAssembly.GetType( "Microsoft.CodeAnalysis.Host.Mef.IMefHostExportProvider" );

        var getExports = iMefHostExportProvider?.GetMethods()
            .SingleOrDefault( m => m.Name == "GetExports" && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 0 );

        var getExportsOfDiagnosticRefresher = getExports?.MakeGenericMethod( iDiagnosticsRefresher );

        if ( iMefHostExportProvider == null || getExportsOfDiagnosticRefresher == null )
        {
            return null;
        }

        // This is equivalent to the following code that uses internal Roslyn types:
        // ((IMefHostExportProvider)hostServices).GetExports<IDiagnosticsRefresher>().SingleOrDefault()?.Value.RequestWorkspaceRefresh();
        // Note that lazyExport is null if there are no exports of IDiagnosticsRefresher, which happens when LanguageServer is not used (i.e. in VS and Rider).

        var hostServices = Expression.Parameter( typeof(HostServices), "hostServices" );
        var castedHostServices = Expression.Convert( hostServices, iMefHostExportProvider );
        var exports = Expression.Call( castedHostServices, getExportsOfDiagnosticRefresher );
        var lazyExport = Expression.Call( typeof(Enumerable), "SingleOrDefault", [typeof(Lazy<>).MakeGenericType( iDiagnosticsRefresher )], exports );
        var lazyExportVariable = Expression.Variable( lazyExport.Type, "lazyExport" );
        var lazyExportAssignment = Expression.Assign( lazyExportVariable, lazyExport );
        var export = Expression.Property( lazyExportVariable, "Value" );
        var requestWorkspaceRefresh = Expression.Call( export, requestWorkspaceRefreshMethod );
        var condition = Expression.NotEqual( lazyExportVariable, Expression.Constant( null, lazyExport.Type ) );
        var ifStatement = Expression.IfThen( condition, requestWorkspaceRefresh );

        var block = Expression.Block( [lazyExportVariable], lazyExportAssignment, ifStatement );

        var lambda = Expression.Lambda<Action<HostServices>>( block, hostServices );

        return lambda.Compile();
    }
}