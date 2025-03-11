// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System;
using System.Reflection;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In PostSharp, this object exposed the run-time execution context to an event interception advice. However, in Metalama, advice do not execute at run time.
    /// Instead, advice are templates that generate run-time code. This run-time code does not need helper objects to represent the execution context.
    /// </summary>
    [PublicAPI]
    public abstract class EventInterceptionArgs : AdviceArgs
    {
        /// <summary>
        /// Use <see cref="IEvent"/>.<see cref="IEventInvoker.Add"/>, <see cref="IEvent"/>.<see cref="IEventInvoker.Remove"/> or <see cref="IEvent"/>.<see cref="IEventInvoker.Raise"/> to generate run-time code that accesses the event.
        /// </summary>
        public abstract IEventBinding Binding { get; }

        /// <summary>
        /// Use the <c>value</c> parameter in the template.
        /// </summary>
        public Delegate Handler { get; }

        /// <summary>
        /// In PostSharp, gets the return value after <see cref="ProceedInvokeHandler"/> has been called. In Metalama, <see cref="ProceedInvokeHandler"/>
        /// is not supported, however you can call the handler yourself (it is exposed as the <c>value</c> template parameter) and store the result in a local
        /// variable.
        /// </summary>
        public object ReturnValue { get; set; }

        /// <summary>
        /// Use <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.Event"/>. If you need a run-time <see cref="EventInfo"/>.
        /// call <see cref="IEvent.ToEventInfo"/>.
        /// </summary>
        public EventInfo Event { get; set; }

        /// <summary>
        /// In PostSharp, gets arguments in an <see cref="EventInterceptionAspect.OnInvokeHandler"/> advice. In Metalama, this advice type is not supported.
        /// </summary>
        public Arguments Arguments { get; }

        /// <summary>
        /// Use <see cref="meta"/>.<see cref="meta.Proceed"/> from the advice that overrides the event adder, or use
        /// <see cref="IEvent"/>.<see cref="IEventInvoker.Add"/>.
        /// </summary>
        public abstract void ProceedAddHandler();

        /// <summary>
        /// Use <see cref="meta"/>.<see cref="meta.Proceed"/> from the advice that overrides the event adder, or use
        /// <see cref="IEvent"/>.<see cref="IEventInvoker.Add"/>.
        /// </summary>
        public abstract void AddHandler( Delegate handler );

        /// <summary>
        /// Use <see cref="meta"/>.<see cref="meta.Proceed"/> from the advice that overrides the event remover, or use
        /// <see cref="IEvent"/>.<see cref="IEventInvoker.Remove"/>.
        /// </summary>
        public abstract void ProceedRemoveHandler();

        /// <summary>
        /// Use <see cref="meta"/>.<see cref="meta.Proceed"/> from the advice that overrides the event remover, or use
        /// <see cref="IEvent"/>.<see cref="IEventInvoker.Remove"/>.
        /// </summary>
        public abstract void RemoveHandler( Delegate handler );

        /// <summary>
        /// Not supported in Metalama.
        /// </summary>
        [Obsolete( "", true )]
        public abstract void ProceedInvokeHandler();

        /// <summary>
        /// Not supported in Metalama.
        /// </summary>
        [Obsolete( "", true )]
        public abstract object InvokeHandler( Delegate handler, Arguments arguments );
    }
}