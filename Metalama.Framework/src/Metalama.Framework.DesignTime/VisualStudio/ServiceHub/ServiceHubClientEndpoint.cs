// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Utilities;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceHub;

/// <summary>
/// The <see cref="ServiceHubClientEndpoint"/> lives in the analysis process. It registers itself to the server endpoint, <see cref="ServiceHubServerEndpoint"/>,
/// which lives in the DevEnv UI process.
/// </summary>
internal sealed class ServiceHubClientEndpoint : ClientEndpoint
{
    public ServiceHubClientEndpoint( GlobalServiceProvider serviceProvider, string pipeName ) : base(
        serviceProvider.Underlying,
        pipeName )
    {
        this.Client = new ServiceHubRpcClient( this );
    }

    protected override IEnumerable<RpcClient> CreateServiceClients() => [this.Client];

    public ServiceHubRpcClient Client { get; }

    public static bool TryStart(
        GlobalServiceProvider serviceProvider,
        [NotNullWhen( true )] out ServiceHubClientEndpoint? serviceHubApiProvider )
    {
        if ( !TryGetPipeName( out var pipeName ) )
        {
            serviceHubApiProvider = null;

            return false;
        }

        var endpoint = new ServiceHubClientEndpoint( serviceProvider, pipeName );
        _ = endpoint.ConnectAsync( serviceProvider.GetRequiredService<ApplicationExitManager>().Token );

        serviceHubApiProvider = endpoint;

        return true;
    }

    private static bool TryGetPipeName( [NotNullWhen( true )] out string? pipeName )
    {
        var parentProcesses = ProcessUtilities.GetParentProcesses();

        Engine.Utilities.Diagnostics.Logger.Remoting.Trace?.Log(
            $"Parent processes: {string.Join( ", ", parentProcesses.SelectAsImmutableArray( x => x.ToString() ) )}" );

        ProcessInfo devEnvProcess;

        if ( parentProcesses.Count >= 2 &&
             string.Equals( parentProcesses[0].ProcessName, "Microsoft.ServiceHub.Controller", StringComparison.OrdinalIgnoreCase ) &&
             string.Equals( parentProcesses[1].ProcessName, "devenv", StringComparison.OrdinalIgnoreCase )
           )
        {
            // VS 2022.
            devEnvProcess = parentProcesses[1];
        }
        else if ( parentProcesses.Count >= 1 && string.Equals( parentProcesses[0].ProcessName, "devenv", StringComparison.OrdinalIgnoreCase ) )
        {
            // VS 2026.
            devEnvProcess = parentProcesses[0];
        }
        else
        {
            Engine.Utilities.Diagnostics.Logger.Remoting.Error?.Log( "The process 'devenv' could not be found. " );
            pipeName = null;

            return false;
        }

        pipeName = PipeNameProvider.GetPipeName( EndpointRole.Discovery, devEnvProcess.ProcessId );

        return true;
    }
}