// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.DesignTime.Rpc.Notifications;

/// <summary>
/// Event data broadcast when an endpoint configuration changes. This event notifies subscribers
/// that they may need to reconnect or refresh their connection to a specific project's services.
/// </summary>
[RpcContract]
public sealed class EndpointChangedEventData : RpcEventData
{
    /// <summary>
    /// Gets the GUID of the project whose endpoint has changed.
    /// </summary>
    public Guid ProjectGuid { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointChangedEventData"/> class.
    /// </summary>
    /// <param name="projectGuid">The GUID of the project whose endpoint has changed.</param>
    public EndpointChangedEventData( Guid projectGuid )
    {
        this.ProjectGuid = projectGuid;
    }

    /// <inheritdoc />
    public override string Category => "";
}