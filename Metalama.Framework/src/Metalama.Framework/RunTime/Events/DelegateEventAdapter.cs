// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.RunTime.Events
{
    /// <summary>
    /// An implementation of <see cref="IEventAdapter{TDelegate,TOwner,TArgs}"/> based on delegates.
    /// </summary>
    /// <typeparam name="TDelegate">The event delegate type.</typeparam>
    /// <typeparam name="TArgs">The event arguments type  (i.e. the arguments of <typeparamref name="TDelegate"/> packed as a tuple).</typeparam>
    /// <typeparam name="TOwner">The type declaring the event.</typeparam>
    public sealed class DelegateEventAdapter<TDelegate, TOwner, TArgs> : IEventAdapter<TDelegate, TOwner, TArgs>
        where TDelegate : Delegate
        where TOwner : class?
    {
        private readonly EventHandlerInvocationCallback<TDelegate, TOwner, TArgs> _invokeHandlerCallback;
        private readonly Func<EventBroker<TDelegate, TOwner, TArgs>, TDelegate> _getBrokerInvocationDelegateCallback;
        private readonly Action<TDelegate, TOwner> _addHandlerCallback;
        private readonly Action<TDelegate, TOwner> _removeHandlerCallback;

        public DelegateEventAdapter(
            EventHandlerInvocationCallback<TDelegate, TOwner, TArgs> invokeHandler,
            Func<EventBroker<TDelegate, TOwner, TArgs>, TDelegate> getBrokerInvocationDelegate,
            Action<TDelegate, TOwner> addHandler,
            Action<TDelegate, TOwner> removeHandler )
        {
            this._invokeHandlerCallback = invokeHandler ?? throw new ArgumentNullException( nameof(invokeHandler) );
            this._getBrokerInvocationDelegateCallback = getBrokerInvocationDelegate ?? throw new ArgumentNullException( nameof(getBrokerInvocationDelegate) );
            this._addHandlerCallback = addHandler ?? throw new ArgumentNullException( nameof(addHandler) );
            this._removeHandlerCallback = removeHandler ?? throw new ArgumentNullException( nameof(removeHandler) );
        }

        void IEventAdapter<TDelegate, TOwner, TArgs>.InvokeHandler( TDelegate handler, TOwner owner, ref TArgs args ) => this._invokeHandlerCallback( handler, owner, ref args );

        void IEventAdapter<TDelegate, TOwner, TArgs>.AddHandler( TDelegate handler, TOwner owner ) => this._addHandlerCallback( handler, owner );

        void IEventAdapter<TDelegate, TOwner, TArgs>.RemoveHandler( TDelegate handler, TOwner owner ) => this._removeHandlerCallback( handler, owner );

        TDelegate IEventAdapter<TDelegate, TOwner, TArgs>.GetBrokerInvocationDelegate( EventBroker<TDelegate, TOwner, TArgs> broker ) => this._getBrokerInvocationDelegateCallback( broker );
    }
}