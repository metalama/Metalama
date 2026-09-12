// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Backstage.Extensibility;

/// <summary>
/// Delivers events from a service to the services that subscribed to them, asynchronously and in the order of
/// publication. A service publishes an event when something happened that other services may react to, without
/// referencing them: for instance, the telemetry service publishes that a report was captured, and the user interface
/// shows a notification.
/// </summary>
/// <remarks>
/// <para>
/// The dispatcher is the mechanism by which the lower packages notify the user interface without depending on it.
/// Every event flows from a lower package to a higher one: the type of the event is declared by the publisher, and the
/// subscriber references the publisher.
/// </para>
/// <para>
/// Delivery is asynchronous: <see cref="Publish{TEvent}"/> returns immediately and the subscribers run later, in the
/// order of publication, on a single queue per service provider. A subscriber therefore never runs on the stack of
/// the publisher, and a publisher on the critical path of a compilation does not pay for the reaction. A test, or a
/// process that is about to exit, waits for the delivery of every published event with <see cref="CompleteAsync"/>.
/// </para>
/// <para>
/// A subscriber that throws does not stop the queue: the exception is reported to the
/// <see cref="IEventDispatcherObserver"/> and to the log. A publication that no service has subscribed to is reported to
/// the observer as well, so that a test can detect it.
/// </para>
/// </remarks>
[PublicAPI]
public interface IEventDispatcher : IBackstageService
{
    /// <summary>
    /// Enqueues an event for delivery to the subscribers of its type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event, declared by the publisher.</typeparam>
    /// <param name="event">The event.</param>
    void Publish<TEvent>( TEvent @event )
        where TEvent : class;

    /// <summary>
    /// Subscribes a handler to the events of a type. The handler runs on the queue of the dispatcher, never on the
    /// stack of the publisher.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="handler">The handler.</param>
    /// <returns>An object whose disposal removes the subscription.</returns>
    IDisposable Subscribe<TEvent>( Action<TEvent> handler )
        where TEvent : class;

    /// <summary>
    /// Waits until every event published so far has been delivered. Events can still be published afterwards.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task CompleteAsync( CancellationToken cancellationToken );
}
