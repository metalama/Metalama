// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code.Invokers
{
    /// <summary>
    /// Allows adding/removing delegates to/from events.
    /// </summary>
    [CompileTime]
    public interface IEventInvoker
    {
        // TODO: Create methods CreateAddExpression, CreateRemoveExpression, CreateRaiseExpression.
        
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