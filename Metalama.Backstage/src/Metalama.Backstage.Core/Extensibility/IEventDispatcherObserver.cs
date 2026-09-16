// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Extensibility;

/// <summary>
/// Observes the failures of the <see cref="IEventDispatcher"/>: a subscriber that threw, and a publication that no
/// service subscribed to. The production registration logs both. A test registers an observer that records them, so
/// that a background handler that failed, or a notification that nobody received, becomes a failing test.
/// </summary>
/// <remarks>
/// The observer is optional. It must not report to telemetry, because the dispatcher is what delivers the telemetry
/// events.
/// </remarks>
[PublicAPI]
public interface IEventDispatcherObserver : IBackstageService
{
    /// <summary>
    /// Invoked when a subscriber threw an exception while handling an event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="exception">The exception.</param>
    void OnSubscriberFailed( object @event, Exception exception );

    /// <summary>
    /// Invoked when an event was published but no service had subscribed to its type.
    /// </summary>
    /// <param name="event">The event.</param>
    void OnEventUnhandled( object @event );
}
