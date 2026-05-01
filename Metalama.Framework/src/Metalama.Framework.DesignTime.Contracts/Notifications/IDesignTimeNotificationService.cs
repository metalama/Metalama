// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using System;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Notifications;

/// <summary>
/// Cross-version-safe entry point for subscribing to design-time notifications. Resolved via
/// <c>ICompilerServiceProvider.GetService(typeof(IDesignTimeNotificationService))</c>.
/// </summary>
/// <remarks>
/// <para>
/// This interface replaces direct consumption of <c>Metalama.Framework.DesignTime.Rpc</c> by cross-version
/// consumers (e.g. the Visual Studio extension). The Metalama-side implementation translates internal RPC
/// events into <see cref="IDesignTimeNotificationEvent"/> instances and delivers them to the observer
/// in-process; no RPC pipe is opened across a Metalama-version boundary.
/// </para>
/// <para>
/// Cross-version contract. <see cref="GuidAttribute"/>, type name, and member signatures are frozen forever.
/// Future evolution must go through new interfaces (e.g. <c>IDesignTimeNotificationService2</c>) with new GUIDs.
/// </para>
/// </remarks>
[ComImport]
[Guid( "78E9D456-59D8-4350-A63E-6B88D439B82E" )]
public interface IDesignTimeNotificationService : ICompilerService
{
    /// <summary>
    /// Subscribes <paramref name="observer"/> to receive events whose <see cref="IDesignTimeNotificationEvent.EventTypeName"/>
    /// is one of <paramref name="eventTypeNames"/>.
    /// </summary>
    /// <param name="observer">The observer to invoke when a matching event is raised.</param>
    /// <param name="eventTypeNames">
    /// The event type names to subscribe to (use the constants in <see cref="DesignTimeNotificationEventTypes"/>).
    /// </param>
    /// <returns>
    /// A disposable that unsubscribes the observer. After <c>Dispose</c>, <paramref name="observer"/> will not be invoked again
    /// (modulo events already in flight).
    /// </returns>
    IDisposable Subscribe( IDesignTimeNotificationObserver observer, string[] eventTypeNames );
}
