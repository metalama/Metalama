// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Notifications;

/// <summary>
/// Base interface for events delivered through <see cref="IDesignTimeNotificationService"/>.
/// Concrete events implement a more specific derived interface (e.g. <see cref="ICompilationResultChangedEvent"/>).
/// </summary>
/// <remarks>
/// This interface is part of the cross-version contract surface. Its <see cref="GuidAttribute"/>, type name,
/// and member signatures are frozen forever. Future evolution must go through new interfaces with new GUIDs.
/// </remarks>
[ComImport]
[Guid( "9BAB53DF-7FED-4757-B589-54E79EEE5A20" )]
public interface IDesignTimeNotificationEvent
{
    /// <summary>
    /// Gets a stable string identifier of the event type, matching one of the constants in
    /// <see cref="DesignTimeNotificationEventTypes"/>. Subscribers can use this for fast dispatch
    /// without relying on type-test against derived interfaces.
    /// </summary>
    string EventTypeName { get; }
}
