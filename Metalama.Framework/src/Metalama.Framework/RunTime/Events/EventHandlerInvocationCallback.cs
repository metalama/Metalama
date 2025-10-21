// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.RunTime.Events;

/// <summary>
/// Delegate for the implementation of the <see cref="IEventAdapter{TDelegate,TArgs,TState}.InvokeHandler"/> method by <see cref="DelegateEventAdapter{TDelegate,TArgs,TState}"/>.
/// </summary>
/// <typeparam name="TDelegate">Type of the delegate.</typeparam>
/// <typeparam name="TArgs">Type of arguments (i.e. the arguments of <typeparamref name="TDelegate"/> packed as a tuple).</typeparam>
/// <typeparam name="TState">An opaque state stored by the <see cref="EventBroker{TDelegate,TArgs,TState}"/> and passed along to delegates.</typeparam>
public delegate void EventHandlerInvocationCallback<in TDelegate, TArgs, in TState>( TDelegate handler, ref TArgs args, TState state )
    where TDelegate : Delegate;