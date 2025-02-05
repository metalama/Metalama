// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Utilities;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Metalama.Framework.Engine.Services;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceHub;

/// <summary>
/// The <see cref="ServiceHubClientEndpoint"/> lives in the analysis process. It registers itself to the server endpoint, <see cref="ServiceHubServerEndpoint"/>,
/// which lives in the DevEnv UI process.
/// </summary>
internal sealed class ServiceHubClientEndpoint : ClientEndpoint, IServiceHubRpcApiClientProvider
{
    public ServiceHubClientEndpoint( GlobalServiceProvider serviceProvider, string pipeName ) : base(
        serviceProvider.Underlying,
        pipeName )
    {
        this.ServiceHubApiClient = new ServiceHubRpcClient( this );
    }

    protected override IEnumerable<RpcClient> CreateServiceClients() => [this.ServiceHubApiClient];

    public ServiceHubRpcClient ServiceHubApiClient { get; }

    public static bool TryStart(
        GlobalServiceProvider serviceProvider,
        CancellationToken cancellationToken,
        [NotNullWhen( true )] out ServiceHubClientEndpoint? serviceHubApiProvider )
    {
        if ( !TryGetPipeName( out var pipeName ) )
        {
            serviceHubApiProvider = null;

            return false;
        }

        var endpoint = new ServiceHubClientEndpoint( serviceProvider, pipeName );
        _ = endpoint.ConnectAsync( cancellationToken );

        serviceHubApiProvider = endpoint;

        return true;
    }

    private static bool TryGetPipeName( [NotNullWhen( true )] out string? pipeName )
    {
        var parentProcesses = ProcessUtilities.GetParentProcesses();

        Engine.Utilities.Diagnostics.Logger.Remoting.Trace?.Log(
            $"Parent processes: {string.Join( ", ", parentProcesses.SelectAsImmutableArray( x => x.ToString() ) )}" );

        if ( parentProcesses.Count < 2 ||
             !string.Equals( parentProcesses[0].ProcessName, "Microsoft.ServiceHub.Controller", StringComparison.OrdinalIgnoreCase ) ||
             !string.Equals( parentProcesses[1].ProcessName, "devenv", StringComparison.OrdinalIgnoreCase )
           )
        {
            Engine.Utilities.Diagnostics.Logger.Remoting.Error?.Log( "The process 'devenv' could not be found. " );
            pipeName = null;

            return false;
        }

        var parentProcess = parentProcesses[1];

        pipeName = PipeNameProvider.GetPipeName( EndpointRole.Discovery, parentProcess.ProcessId );

        return true;
    }
}