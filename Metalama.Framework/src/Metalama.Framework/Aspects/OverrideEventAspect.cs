// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base aspect that overrides the implementation of an event by providing template methods for add, remove, and invoke operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class simplifies creating aspects that override event behavior. Derived classes can override the template methods
    /// <see cref="OverrideAdd"/>, <see cref="OverrideRemove"/>, and <see cref="OverrideInvoke"/> to customize event semantics.
    /// All template methods are optional (marked with <c>[Template(IsEmpty = true)]</c>).
    /// </para>
    /// <para>
    /// When applied to a field-like event, it is automatically transformed into an explicitly implemented event with a backing field,
    /// similar to how auto-properties are transformed when overridden.
    /// </para>
    /// <para>
    /// The <see cref="OverrideInvoke"/> template is invoked <strong>once per event handler</strong> when the event is raised.
    /// For example, if there are 3 event handlers and the event is invoked once, <see cref="OverrideInvoke"/> will be called 3 times.
    /// This enables per-handler interception patterns such as exception handling with handler removal, async execution, or handler filtering.
    /// Use <c>meta.Proceed()</c> to invoke the actual handler. You can remove handlers from within the template using
    /// <see cref="Metalama.Framework.Code.Invokers.IEventInvoker.Remove(dynamic?)">meta.Target.Event.Remove(handler)</see>.
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong> Delegate signatures with non-void return types or with <c>ref</c>/<c>out</c> parameters are not supported.
    /// Only handlers added through the event's add and remove accessors are intercepted; handlers added directly to the backing field
    /// (if accessible) will not be intercepted.
    /// </para>
    /// <para>
    /// <strong>Performance Note:</strong> Overriding the invoke operation uses an
    /// <see cref="Metalama.Framework.RunTime.Events.EventBroker{TImplementation, THandler, TArgs}"/> pattern which adds memory overhead
    /// (one broker instance per event per object instance, unless the event is static) and allocates short-term memory during event invocation.
    /// Additional type conversions (casts) are also required. This may affect performance for high-frequency events, although high-frequency
    /// events are not a common use case for .NET events.
    /// </para>
    /// </remarks>
    /// <seealso cref="EventAspect"/>
    /// <seealso cref="AdviserExtensions.OverrideAccessors(IAdviser{IEvent}, string?, string?, string?, string?, object?, object?)"/>
    /// <seealso href="@overriding-events"/>
    [AttributeUsage( AttributeTargets.Event )]
    public abstract class OverrideEventAspect : EventAspect
    {
        /// <inheritdoc />
        public override void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            builder.OverrideAccessors(
                nameof(this.OverrideAdd),
                nameof(this.OverrideRemove),
                null,
                nameof(this.OverrideInvoke) );
        }

        /// <summary>
        /// Template method for overriding the event add accessor.
        /// </summary>
        /// <param name="handler">The event handler being added. Use <c>dynamic</c> to support any delegate type.</param>
        /// <remarks>
        /// <para>
        /// This template method is optional (marked with <c>[Template(IsEmpty = true)]</c>). If not overridden,
        /// the default add behavior is used.
        /// </para>
        /// <para>
        /// Within the template, use <c>meta.Target.Event</c> to access event metadata. To delegate to the original
        /// add implementation, use <c>meta.Target.Event.Add(handler)</c> or <c>meta.Proceed()</c>.
        /// </para>
        /// </remarks>
        [Template( IsEmpty = true )]
        public virtual void OverrideAdd( dynamic handler ) => throw new NotImplementedException();

        /// <summary>
        /// Template method for overriding the event remove accessor.
        /// </summary>
        /// <param name="handler">The event handler being removed. Use <c>dynamic</c> to support any delegate type.</param>
        /// <remarks>
        /// <para>
        /// This template method is optional (marked with <c>[Template(IsEmpty = true)]</c>). If not overridden,
        /// the default remove behavior is used.
        /// </para>
        /// <para>
        /// Within the template, use <c>meta.Target.Event</c> to access event metadata. To delegate to the original
        /// remove implementation, use <c>meta.Target.Event.Remove(handler)</c> or <c>meta.Proceed()</c>.
        /// </para>
        /// </remarks>
        [Template( IsEmpty = true )]
        public virtual void OverrideRemove( dynamic handler ) => throw new NotImplementedException();

        /// <summary>
        /// Template method for overriding event invocation behavior, executed once per handler when the event is raised.
        /// </summary>
        /// <param name="handler">The specific event handler being invoked. Use <c>dynamic</c> to support any delegate type.</param>
        /// <returns>The return value from invoking the handler (typically <c>null</c> for void handlers).</returns>
        /// <remarks>
        /// <para>
        /// This template method is optional (marked with <c>[Template(IsEmpty = true)]</c>). If not overridden,
        /// handlers are invoked directly without interception.
        /// </para>
        /// <para>
        /// This template is invoked <strong>once for each registered handler</strong> when the event is raised.
        /// For example, if there are 3 event handlers and the event is raised once, this template will be called 3 times.
        /// This enables per-handler interception patterns such as exception handling with handler removal, async execution,
        /// or handler filtering.
        /// </para>
        /// <para>
        /// Use <c>meta.Proceed()</c> to invoke the actual handler. Within the template, you can access event metadata via
        /// <c>meta.Target.Event</c> and remove handlers programmatically using
        /// <see cref="Metalama.Framework.Code.Invokers.IEventInvoker.Remove(dynamic?)">meta.Target.Event.Remove(handler)</see>.
        /// Note that <see cref="Metalama.Framework.Code.Invokers.IEventInvoker.Raise(dynamic?[])">meta.Target.Event.Raise()</see> is not
        /// supported from this template; you must use <c>meta.Proceed()</c>.
        /// </para>
        /// </remarks>
        [Template( IsEmpty = true )]
        public virtual dynamic? OverrideInvoke( dynamic handler ) => throw new NotImplementedException();

        // TODO: Enable when support for overriding raise is added.
        // [Template( IsEmpty = true )]
        // public virtual dynamic? OverrideRaise() => throw new NotImplementedException();

        public override void BuildEligibility( IEligibilityBuilder<IEvent> builder )
        {
            builder.AddRule( EligibilityRuleFactory.OverrideEventAdviceRule );
        }
    }
}