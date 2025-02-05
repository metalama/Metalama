// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Invokers;
using System.Collections.Generic;
using System.Reflection;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represent an event.
    /// </summary>
    public interface IEvent : IHasAccessors, IEventInvoker
    {
        /// <summary>
        /// Gets the type of the event, i.e. the type of the delegate.
        /// </summary>
        new INamedType Type { get; }

        IMethod Signature { get; }

        /// <summary>
        /// Gets the method implementing the <c>add</c> semantic. In case of field-like events, this property returns
        /// an object that does not map to source code but allows to add aspects and advice as with a normal method.
        /// </summary>
        IMethod AddMethod { get; }

        /// <summary>
        /// Gets the method implementing the <c>remove</c> semantic. In case of field-like events, this property returns
        /// an object that does not map to source code but allows to add aspects and advice as with a normal method.
        /// </summary>
        IMethod RemoveMethod { get; }

        /// <summary>
        /// Gets an object that represents the <c>raise</c> semantic and allows to add aspects and advice
        /// as with a normal method.
        /// </summary>
        IMethod? RaiseMethod { get; }

        /// <summary>
        /// Gets the base event that is overridden by the current event.
        /// </summary>
        IEvent? OverriddenEvent { get; }

        /// <summary>
        /// Gets a list of interface events this event explicitly implements.
        /// </summary>
        IReadOnlyList<IEvent> ExplicitInterfaceImplementations { get; }

        /// <summary>
        /// Gets the definition of the event. If the current declaration is an event of
        /// a generic type instance, this returns the event in the generic type definition. Otherwise, it returns the current instance.
        /// </summary>
        new IEvent Definition { get; }

        /// <summary>
        /// Gets an <see cref="EventInfo"/> that represents the current event at run time.
        /// </summary>
        /// <returns>An <see cref="EventInfo"/> that can be used only in run-time code.</returns>
        EventInfo ToEventInfo();

        new IRef<IEvent> ToRef();

        /// <summary>
        /// Gets the initializer expression (i.e. the expression at the right hand of the equal sign) of the field-like event, if any.
        /// When the event is defined in source code, this property returns an <see cref="ISourceExpression"/>, which
        /// exposes a <see cref="TypedConstant"/> when possible.
        /// </summary>
        IExpression? InitializerExpression { get; }
    }
}