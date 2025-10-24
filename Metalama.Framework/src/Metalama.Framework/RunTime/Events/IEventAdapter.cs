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
/// <typeparam name="TArgs">A tuple type packing event arguments.</typeparam>
/// <typeparam name="TState">An opaque state stored by <see cref="EventBroker{TDelegate,TArgs,TState}"/>.</typeparam>
public interface IEventAdapter<TDelegate, TArgs, TState>
    where TDelegate : Delegate
{
    /// <summary>
    /// Invokes an event handler.
    /// </summary>
    /// <param name="handler">An event handler.</param>
    /// <param name="args">A tuple packing all arguments to be passed to the event <paramref name="handler"/>.</param>
    void InvokeHandler( TDelegate handler, ref TArgs args, TState state );

    /// <summary>
    /// Adds a handler to the event. Typically used to register the <see cref="EventBroker{TDelegate,TOwner,TArgs}"/> to the event.
    /// </summary>
    /// <param name="handler">An event handler (typically a method of the <see cref="EventBroker{TDelegate,TOwner,TArgs}"/>).</param>
    void AddHandler( TDelegate handler, TState state );

    /// <summary>
    /// Removes a handler from the event. Typically used to unregister the <see cref="EventBroker{TDelegate,TOwner,TArgs}"/> from the event.
    /// </summary>
    /// <param name="handler">An event handler (typically a method of the <see cref="EventBroker{TDelegate,TOwner,TArgs}"/>).</param>
    void RemoveHandler( TDelegate handler, TState state );

    /// <summary>
    /// Gets the delegate that packs the event arguments and calls <see cref="EventBroker{TDelegate,TOwner,TArgs}.Invoke"/>
    /// or <see cref="EventBroker{TDelegate,TOwner,TArgs}.InvokeByRef"/>. This delegate is then registered in the event using <see cref="AddHandler"/>
    /// or unregistered using <see cref="RemoveHandler"/>.
    /// </summary>
    /// <param name="broker">The <see cref="EventBroker{TDelegate,TOwner,TArgs}"/> that should be invoked.</param>
    /// <returns>A delegate.</returns>
    TDelegate GetBrokerInvocationDelegate( EventBroker<TDelegate, TArgs, TState> broker );
}