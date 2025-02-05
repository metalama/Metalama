// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceHub;

[PublicAPI]
public interface IServiceHubRpcApi : IRpcApi
{
    Task RegisterAnalysisServiceAsync( string pipeName, CancellationToken cancellationToken );

    Task RegisterAnalysisServiceProjectAsync( ProjectKey projectKey, string pipeName, [UsedImplicitly] CancellationToken cancellationToken );

    Task UnregisterAnalysisServiceAsync( string pipeName, CancellationToken cancellationToken );

    Task UnregisterAnalysisServiceProjectAsync( ProjectKey projectKey, CancellationToken cancellationToken );
}