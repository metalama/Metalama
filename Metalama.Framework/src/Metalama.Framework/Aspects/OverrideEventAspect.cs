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
    /// The <see cref="OverrideInvoke"/> template is particularly powerful as it is invoked once per event handler when the event is raised.
    /// This allows you to implement patterns like exception handling, handler removal, or async execution per handler.
    /// Use <c>meta.Proceed()</c> to invoke the actual handler.
    /// </para>
    /// <para>
    /// When applied to a field-like event, it is automatically transformed into an explicitly implemented event with a backing field,
    /// similar to how auto-properties are transformed.
    /// </para>
    /// <para>
    /// <strong>Performance Note:</strong> Overriding the invoke operation uses an <see cref="Metalama.Framework.RunTime.Events.EventBroker{TImplementation, THandler, TArgs}"/>
    /// pattern which adds memory overhead (one broker instance per event per object instance) and allocates memory during event invocation.
    /// This may affect performance for high-frequency events.
    /// </para>
    /// <para>
    /// Access event metadata via <c>meta.Target.Event</c>. You can add/remove handlers programmatically using
    /// <c>meta.Target.Event.Add(handler)</c> and <c>meta.Target.Event.Remove(handler)</c>.
    /// </para>
    /// </remarks>
    /// <seealso cref="EventAspect"/>
    /// <seealso cref="AdviserExtensions.OverrideAccessors"/>
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

        [Template( IsEmpty = true )]
        public virtual void OverrideAdd( dynamic handler ) => throw new NotImplementedException();

        [Template( IsEmpty = true )]
        public virtual void OverrideRemove( dynamic handler ) => throw new NotImplementedException();

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