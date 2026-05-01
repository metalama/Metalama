// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.Notifications;

namespace Metalama.Framework.DesignTime.VisualStudio.Notifications;

internal sealed class EndpointChangedEvent : IEndpointChangedEvent
{
    public EndpointChangedEvent( Guid projectGuid )
    {
        this.ProjectGuid = projectGuid;
    }

    public string EventTypeName => DesignTimeNotificationEventTypes.EndpointChanged;

    public Guid ProjectGuid { get; }
}
