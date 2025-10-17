// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.RunTime.Events;

/// <summary>
/// Signature of the callback invoked by <see cref="EventBroker{TDelegate,TOwner,TArgs}"/> into an aspect-generated method in the owner type.
/// </summary>
/// <typeparam name="TDelegate">Type of the delegate.</typeparam>
/// <typeparam name="TOwner">Type of the class declaring the event.</typeparam>
/// <typeparam name="TArgs">Type of arguments (i.e. the arguments of <typeparamref name="TDelegate"/> packed as a tuple).</typeparam>
public delegate void EventHandlerInvocationCallback<in TDelegate, in TOwner, TArgs>( TDelegate handler, TOwner owner, ref TArgs args )
    where TDelegate : Delegate
    where TOwner : class?;