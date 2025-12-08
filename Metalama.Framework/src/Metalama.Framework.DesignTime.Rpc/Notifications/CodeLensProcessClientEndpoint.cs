// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.DesignTime.Rpc.Notifications;

/// <summary>
/// Client endpoint for the CodeLens process. Connects to the event hub to receive
/// compilation and endpoint change notifications.
/// </summary>
[PublicAPI]
public sealed class CodeLensProcessClientEndpoint : ClientEndpoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeLensProcessClientEndpoint"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="pipeName">The name of the pipe to connect to.</param>
    public CodeLensProcessClientEndpoint( IServiceProvider serviceProvider, string pipeName ) : base(
        serviceProvider,
        pipeName )
    {
        this.Client = new CodeLensProcessRpcClient( this, serviceProvider );
    }

    /// <summary>
    /// Gets the RPC client for event hub communication.
    /// </summary>
    public CodeLensProcessRpcClient Client { get; }

    /// <inheritdoc />
    protected override IEnumerable<RpcClient> CreateServiceClients() => [this.Client];
}