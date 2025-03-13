// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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