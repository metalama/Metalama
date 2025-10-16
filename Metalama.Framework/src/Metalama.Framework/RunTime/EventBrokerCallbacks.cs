// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.RunTime
{
    /// <summary>
    /// Represents a set of instance-independent delegates used by an <see cref="EventBroker{TDelegate,TOwner,TArgs}"/>.
    /// </summary>
    /// <typeparam name="TDelegate">The event delegate type.</typeparam>
    /// <typeparam name="TArgs">The event arguments type  (i.e. the arguments of <typeparamref name="TDelegate"/> packed as a tuple).</typeparam>
    /// <typeparam name="TOwner">The type declaring the event.</typeparam>
    public sealed class EventBrokerCallbacks<TDelegate, TOwner, TArgs>
        where TDelegate : Delegate
        where TOwner : class
    {
        /// <summary>
        /// Gets the delegate that invokes a single handler.
        /// </summary>
        public EventHandlerInvocationCallback<TDelegate, TOwner, TArgs> InvokeHandler { get; }

        /// <summary>
        /// Gets the delegate that returns a delegate which calls the <see cref="EventBroker{TDelegate,TOwner,TArgs}.Invoke" /> method.
        /// </summary>
        public Func<EventBroker<TDelegate, TOwner, TArgs>, TDelegate> GetBrokerInvocationDelegate { get; }

        /// <summary>
        /// Gets the delegate that adds an event handler to the target event.
        /// </summary>
        public Action<TDelegate, TOwner> AddHandler { get; }

        /// <summary>
        /// Gets the delegate that removes an event handler from the target event.
        /// </summary>
        public Action<TDelegate, TOwner> RemoveHandler { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventBrokerCallbacks{TDelegate,TOwner,TArgs}"/> class.
        /// </summary>
        public EventBrokerCallbacks(
            EventHandlerInvocationCallback<TDelegate, TOwner, TArgs> invokeHandler,
            Func<EventBroker<TDelegate, TOwner, TArgs>, TDelegate> getBrokerInvocationDelegate,
            Action<TDelegate, TOwner> addHandler,
            Action<TDelegate, TOwner> removeHandler )
        {
            this.InvokeHandler = invokeHandler ?? throw new ArgumentNullException( nameof(invokeHandler) );
            this.GetBrokerInvocationDelegate = getBrokerInvocationDelegate ?? throw new ArgumentNullException( nameof(getBrokerInvocationDelegate) );
            this.AddHandler = addHandler ?? throw new ArgumentNullException( nameof(addHandler) );
            this.RemoveHandler = removeHandler ?? throw new ArgumentNullException( nameof(removeHandler) );
        }
    }
}