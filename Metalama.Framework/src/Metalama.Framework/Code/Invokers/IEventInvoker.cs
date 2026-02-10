// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code.Invokers
{
    /// <summary>
    /// Allows generating run-time code that adds handlers to, removes handlers from, or raises an event when you have its compile-time <see cref="IEvent"/> representation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Invokers are APIs that allow you to generate run-time code from compile-time declarations. When you have an <see cref="IEvent"/>
    /// object (a compile-time representation of an event), you can use its invoker methods to create expressions that add or remove
    /// event handlers, or raise the event. These operations can then be used in templates and will be expanded into actual C# code.
    /// </para>
    /// <para>
    /// The <see cref="Add(dynamic?)"/> and <see cref="Remove(dynamic?)"/> methods generate code to manage event handlers, while <see cref="Raise(dynamic?[])"/>
    /// generates code to invoke the event. Use <see cref="WithObject(dynamic?)"/> to specify the target instance and <see cref="WithOptions"/>
    /// to control nullability behavior and which implementation layer (base, current, or final) to access.
    /// </para>
    /// <para>
    /// By default, the event is accessed on the current object (<c>this</c>) for instance events. The <c>base</c> implementation
    /// is accessed, i.e. the implementation <i>before</i> the current aspect layer.
    /// </para>
    /// </remarks>
    /// <seealso cref="IEvent"/>
    /// <seealso cref="InvokerOptions"/>
    /// <seealso cref="IExpression"/>
    /// <seealso href="@invokers"/>
    /// <seealso href="@dynamic-typing"/>
    [CompileTime]
    public interface IEventInvoker
    {
        // TODO: Create methods CreateAddExpression, CreateRemoveExpression, CreateRaiseExpression.

        /// <summary>
        /// Gets a value indicating whether the event can be raised.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In C#, only field-like events (events without explicit <c>add</c>/<c>remove</c> accessors) can be raised
        /// because they have a backing delegate field that can be invoked. For non-field-like events (events with explicit accessors),
        /// there is no backing delegate to invoke, so this property returns <c>false</c>.
        /// </para>
        /// <para>
        /// When this property returns <c>false</c>, calling <see cref="Raise(dynamic?[])"/> will throw an <see cref="InvalidOperationException"/>,
        /// and <see cref="IEvent.RaiseMethod"/> will return <c>null</c>.
        /// </para>
        /// </remarks>
        bool CanRaise { get; }

        /// <summary>
        /// Generates run-time code that adds a handler, given as run-time C# expression, to the event.
        /// </summary>
        /// <param name="handler">A C# expression representing the event handler. If the compile-time type
        /// of the expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</param>
        /// <returns>An internal Metalama statement object representing the event handler addition. It should be ignored in user code.</returns>
        /// <remarks>
        /// By default, the event is accessed on the current object (<c>this</c>), unless the event is static. The <c>base</c> implementation 
        /// of the event is invoked, i.e. the implementation <i>before</i> the current aspect layer. To change the default values,
        /// use the <see cref="WithOptions"/> method.
        /// </remarks>
        dynamic Add( dynamic? handler );

        /// <summary>
        /// Generates run-time code that adds a handler, given as an <see cref="IExpression"/>, to the event.
        /// </summary>
        /// <param name="handler">A compile-time <see cref="IExpression"/> representing the event handler.</param>
        /// <returns>An internal Metalama statement object representing the event handler addition. It should be ignored in user code.</returns> 
        /// <remarks>
        /// By default, the event is accessed on the current object (<c>this</c>), unless the event is static. The <c>base</c> implementation 
        /// of the event is invoked, i.e. the implementation <i>before</i> the current aspect layer. To change the default values,
        /// use the <see cref="WithOptions"/> method.
        /// </remarks>
        dynamic Add( IExpression handler );

        /// <summary>
        /// Generates run-time code that removes a handler, given a run-time C# expression, from the event. 
        /// </summary>
        /// <param name="handler">A run-time C# expression representing the event handler. If the compile-time type
        /// of the expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</param>
        /// <returns>An internal Metalama statement object representing the event handler addition. It should be ignored in user code.</returns> 
        /// <remarks>
        /// By default, the event is accessed on the current object (<c>this</c>), unless the event is static. The <c>base</c> implementation 
        /// of the event is invoked, i.e. the implementation <i>before</i> the current aspect layer. To change the default values,
        /// use the <see cref="WithOptions"/> method.
        /// </remarks>
        dynamic Remove( dynamic? handler );

        /// <summary>
        /// Generates run-time code that removes a handler, given as an <see cref="IExpression"/>, from the event.
        /// </summary>
        /// <param name="handler">A compile-time <see cref="IExpression"/> representing the event handler.</param>
        /// <returns>An internal Metalama statement object representing the event handler addition. It should be ignored in user code.</returns> 
        /// <remarks>
        /// By default, the event is accessed on the current object (<c>this</c>), unless the event is static. The <c>base</c> implementation 
        /// of the event is invoked, i.e. the implementation <i>before</i> the current aspect layer. To change the default values,
        /// use the <see cref="WithOptions"/> method.
        /// </remarks>
        dynamic Remove( IExpression handler );

        /// <summary>
        /// Generates run-time code that raises the current event with arguments specified as run-time C# expressions.
        /// </summary>
        /// <param name="args">A list of run-time C# expressions to be passed to the event as arguments. If the compile-time type
        /// of any expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</param>
        /// <returns>A dynamic object representing the return value of the event, if any.  If the compile-time type
        /// of any expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</returns>
        /// <remarks>
        /// By default, the event is accessed on the current object (<c>this</c>), unless the event is static. The <c>base</c> implementation 
        /// of the event is invoked, i.e. the implementation <i>before</i> the current aspect layer. To change the default values,
        /// use the <see cref="WithOptions"/> method.
        /// </remarks>
        dynamic? Raise( params dynamic?[] args );

        /// <summary>
        /// Generates run-time code that raises the current event with arguments specified as compile-time <see cref="IExpression"/> objects.
        /// </summary>
        /// <param name="args">A list of run-time C# expressions to be passed to the event as arguments. If the compile-time type
        /// of any expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</param>
        /// <returns>A dynamic object representing the return value of the event, if any.  If the compile-time type
        /// of any expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</returns>
        /// <remarks>
        /// By default, the event is accessed on the current object (<c>this</c>), unless the event is static. The <c>base</c> implementation 
        /// of the event is invoked, i.e. the implementation <i>before</i> the current aspect layer. To change the default values,
        /// use the <see cref="WithOptions"/> method.
        /// </remarks>
        dynamic? Raise( params IExpression[] args );

        /// <summary>
        /// Gets an <see cref="IEventInvoker"/> for the same event and target but with different options.
        /// </summary>
        IEventInvoker WithOptions( InvokerOptions options );

        /// <summary>
        /// Gets an <see cref="IEventInvoker"/> for the same event but with a different object, specified as a compile-time <see cref="IExpression"/> object.
        /// </summary>
        /// <param name="obj">The run-time expression that represents the object on which the field or property is accessed, or <c>null</c> it is static.
        /// </param>
        IEventInvoker WithObject( IExpression? obj );

        /// <summary>
        /// Gets an <see cref="IEventInvoker"/> for the same event but with a different object, specified as a run-time C# expression.
        /// </summary>
        /// <param name="obj">The run-time expression that represents the object on which the field or property is accessed, or <c>null</c> it is static.
        /// If the compile-time type of the expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.
        /// </param>
        IEventInvoker WithObject( dynamic? obj );

        [Obsolete( "Use the WithOptions method." )]
        IEventInvoker With( InvokerOptions options );

        [Obsolete( "Use the WithObject and WithOptions methods." )]
        IEventInvoker With( dynamic? target, InvokerOptions options = default );
    }
}