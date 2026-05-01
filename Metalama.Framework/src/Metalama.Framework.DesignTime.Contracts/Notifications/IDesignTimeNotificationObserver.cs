// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Notifications;

/// <summary>
/// Callback interface implemented by consumers of <see cref="IDesignTimeNotificationService"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="OnEvent"/> is invoked from a background thread; implementations must be thread-safe and should
/// dispatch heavy work onto their own scheduler instead of blocking the caller.
/// </para>
/// <para>
/// Cross-version contract. <see cref="GuidAttribute"/>, type name, and member signatures are frozen forever.
/// </para>
/// </remarks>
[ComImport]
[Guid( "3CAE9F74-193F-4502-BD81-B0ABBEC44559" )]
public interface IDesignTimeNotificationObserver
{
    /// <summary>
    /// Invoked when an event matching the observer's subscription is published.
    /// </summary>
    /// <param name="notificationEvent">The event. Test the runtime type or check <see cref="IDesignTimeNotificationEvent.EventTypeName"/> to dispatch.</param>
    void OnEvent( IDesignTimeNotificationEvent notificationEvent );
}
