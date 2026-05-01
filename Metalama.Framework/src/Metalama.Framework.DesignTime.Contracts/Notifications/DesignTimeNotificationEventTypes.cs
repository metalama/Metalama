// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.DesignTime.Contracts.Notifications;

/// <summary>
/// Stable string identifiers of events that can flow through <see cref="IDesignTimeNotificationService"/>.
/// Used both for subscription (<see cref="IDesignTimeNotificationService.Subscribe"/>) and for dispatch
/// (<see cref="IDesignTimeNotificationEvent.EventTypeName"/>).
/// </summary>
/// <remarks>
/// The string values are part of the cross-version contract and are frozen forever.
/// </remarks>
public static class DesignTimeNotificationEventTypes
{
    /// <summary>
    /// Event type name for <see cref="ICompilationResultChangedEvent"/>.
    /// </summary>
    public const string CompilationResultChanged = "CompilationResultChanged";

    /// <summary>
    /// Event type name for <see cref="IEndpointChangedEvent"/>.
    /// </summary>
    public const string EndpointChanged = "EndpointChanged";
}
