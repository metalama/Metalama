// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.RunTime
{
    /// <summary>
    /// Represents a set of instance-independent delegates used by an <see cref="ActionEventBroker{TDelegate, TArgs}"/>.
    /// </summary>
    public sealed class ActionEventBrokerCallbacks<TDelegate, TArgs>
        where TDelegate : Delegate
    {
        /// <summary>
        /// Gets the delegate that invokes a single handler.
        /// </summary>
        public Action<TDelegate, object, TArgs> InvokeHandler { get; }

        /// <summary>
        /// Gets the delegate that returns a delegate which calls the <see cref="ActionEventBroker{TDelegate, TArgs}.Invoke" /> method.
        /// </summary>
        public Func<ActionEventBroker<TDelegate, TArgs>, TDelegate> GetBrokerInvocationDelegate { get; }

        /// <summary>
        /// Gets the delegate that adds an event handler to the target event.
        /// </summary>
        public Action<TDelegate, object> AddHandler { get; }

        /// <summary>
        /// Gets the the delegate that removes an event handler from the target event.
        /// </summary>
        public Action<TDelegate, object> RemoveHandler { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionEventBrokerCallbacks{TDelegate, TArgs}"/> class.
        /// </summary>
        public ActionEventBrokerCallbacks(
            Action<TDelegate, object, TArgs> invokeHandler,
            Func<ActionEventBroker<TDelegate, TArgs>, TDelegate> getBrokerInvocationDelegate,
            Action<TDelegate, object> addHandler,
            Action<TDelegate, object> removeHandler)
        {
            this.InvokeHandler = invokeHandler ?? throw new ArgumentNullException(nameof(invokeHandler));
            this.GetBrokerInvocationDelegate = getBrokerInvocationDelegate ?? throw new ArgumentNullException(nameof(getBrokerInvocationDelegate));
            this.AddHandler = addHandler ?? throw new ArgumentNullException(nameof(addHandler));
            this.RemoveHandler = removeHandler ?? throw new ArgumentNullException(nameof(removeHandler));
        }
    }
}