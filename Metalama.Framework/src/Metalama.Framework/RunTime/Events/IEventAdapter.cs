// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.RunTime.Events;

/// <summary>
/// A bidirectional adapter between an event, with an arbitrary, and an <see cref="EventBroker{TDelegate,TOwner,TArgs}"/>,
/// where all arguments and return values are packed in a single tuple.
/// </summary>
/// <typeparam name="TDelegate">The event type (a <see cref="Delegate"/>).</typeparam>
/// <typeparam name="TOwner">The declaring type of the event.</typeparam>
/// <typeparam name="TArgs">A tuple type packing event arguments.</typeparam>
public interface IEventAdapter<TDelegate, TOwner, TArgs>
    where TDelegate : Delegate
    where TOwner : class?
{
    /// <summary>
    /// Invokes an event handler.
    /// </summary>
    /// <param name="handler">An event handler.</param>
    /// <param name="owner">The instance owning the event, or <c>null</c> if the event is static.</param>
    /// <param name="args">A tuple packing all arguments to be passed to the event <paramref name="handler"/>.</param>
    void InvokeHandler( TDelegate handler, TOwner owner, ref TArgs args );

    /// <summary>
    /// Adds a handler to the event. Typically used to register the <see cref="EventBroker{TDelegate,TOwner,TArgs}"/> to the event.
    /// </summary>
    /// <param name="handler">An event handler (typically a method of the <see cref="EventBroker{TDelegate,TOwner,TArgs}"/>).</param>
    /// <param name="owner">The instance owning the event, or <c>null</c> if the event is static.</param>
    void AddHandler( TDelegate handler, TOwner owner );

    /// <summary>
    /// Removes a handler from the event. Typically used to unregister the <see cref="EventBroker{TDelegate,TOwner,TArgs}"/> from the event.
    /// </summary>
    /// <param name="handler">An event handler (typically a method of the <see cref="EventBroker{TDelegate,TOwner,TArgs}"/>).</param>
    /// <param name="owner">The instance owning the event, or <c>null</c> if the event is static.</param>
    void RemoveHandler( TDelegate handler, TOwner owner );

    /// <summary>
    /// Gets the delegate that packs the event arguments and calls <see cref="EventBroker{TDelegate,TOwner,TArgs}.Invoke"/>
    /// or <see cref="EventBroker{TDelegate,TOwner,TArgs}.InvokeByRef"/>. This delegate is then registered in the event using <see cref="AddHandler"/>
    /// or unregistered using <see cref="RemoveHandler"/>.
    /// </summary>
    /// <param name="broker">The <see cref="EventBroker{TDelegate,TOwner,TArgs}"/> that should be invoked.</param>
    /// <returns>A delegate.</returns>
    TDelegate GetBrokerInvocationDelegate( EventBroker<TDelegate, TOwner, TArgs> broker );
}