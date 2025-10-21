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
    /// <typeparam name="TState">An opaque state stored by the <see cref="EventBroker{TDelegate,TArgs,TState}"/> and passed along to delegates.</typeparam>
    public sealed class DelegateEventAdapter<TDelegate, TArgs, TState> : IEventAdapter<TDelegate, TArgs, TState>
        where TDelegate : Delegate
    {
        private readonly EventHandlerInvocationCallback<TDelegate, TArgs, TState> _invokeHandlerCallback;
        private readonly Func<EventBroker<TDelegate, TArgs, TState>, TDelegate> _getBrokerInvocationDelegateCallback;
        private readonly Action<TDelegate, TState> _addHandlerCallback;
        private readonly Action<TDelegate, TState> _removeHandlerCallback;

        public DelegateEventAdapter(
            EventHandlerInvocationCallback<TDelegate, TArgs, TState> invokeHandler,
            Func<EventBroker<TDelegate, TArgs, TState>, TDelegate> getBrokerInvocationDelegate,
            Action<TDelegate, TState> addHandler,
            Action<TDelegate, TState> removeHandler )
        {
            this._invokeHandlerCallback = invokeHandler ?? throw new ArgumentNullException( nameof(invokeHandler) );
            this._getBrokerInvocationDelegateCallback = getBrokerInvocationDelegate ?? throw new ArgumentNullException( nameof(getBrokerInvocationDelegate) );
            this._addHandlerCallback = addHandler ?? throw new ArgumentNullException( nameof(addHandler) );
            this._removeHandlerCallback = removeHandler ?? throw new ArgumentNullException( nameof(removeHandler) );
        }

        void IEventAdapter<TDelegate, TArgs, TState>.InvokeHandler( TDelegate handler, ref TArgs args, TState state )
            => this._invokeHandlerCallback( handler, ref args, state );

        void IEventAdapter<TDelegate, TArgs, TState>.AddHandler( TDelegate handler, TState state ) => this._addHandlerCallback( handler, state );

        void IEventAdapter<TDelegate, TArgs, TState>.RemoveHandler( TDelegate handler, TState state ) => this._removeHandlerCallback( handler, state );

        TDelegate IEventAdapter<TDelegate, TArgs, TState>.GetBrokerInvocationDelegate( EventBroker<TDelegate, TArgs, TState> broker )
            => this._getBrokerInvocationDelegateCallback( broker );
    }
}