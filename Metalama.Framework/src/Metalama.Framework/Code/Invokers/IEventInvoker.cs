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
        /// <summary>
        /// Generates run-time code that adds a given handler to the event. By default, the target instance
        /// of the event is <c>this</c> unless the event is static, and the <c>base</c> implementation of the event is invoked,
        /// i.e. the implementation before the current aspect layer. To change the default values, or to use the <c>?.</c> null-conditional operator,
        /// use the <see cref="WithOptions"/> method.
        /// </summary>
        dynamic Add( dynamic? handler );

        /// <summary>
        /// Generates run-time code that removes a given handler from the event. By default, the target instance
        /// of the event is <c>this</c> unless the event is static, and the <c>base</c> implementation of the event is invoked,
        /// i.e. the implementation before the current aspect layer. To change the default values, or to use the <c>?.</c> null-conditional operator,
        /// use the <see cref="WithOptions"/> method.
        /// </summary>
        dynamic Remove( dynamic? handler );

        /// <summary>
        /// Generates run-time code that raises the current event with specified arguments. By default, the target instance
        /// of the event is <c>this</c> unless the event is static, and the <c>base</c> implementation of the event is invoked,
        /// i.e. the implementation before the current aspect layer. To change the default values, or to use the <c>?.</c> null-conditional operator,
        /// use the <see cref="WithOptions"/> method.
        /// </summary>
        dynamic? Raise( params dynamic?[] args );

        /// <summary>
        /// Gets an <see cref="IEventInvoker"/> for the same event and target but with different options.
        /// </summary>
        IEventInvoker WithOptions( InvokerOptions options );

        /// <summary>
        /// Gets an <see cref="IEventInvoker"/> for the same event but with a different object, specified as an <see cref="IExpression"/>.
        /// </summary>
        /// <param name="target">The run-time expression that represents the target instance of the method.
        /// </param>
        IEventInvoker WithObject( IExpression? target );

        IEventInvoker WithObject( dynamic? target );

        [Obsolete( "Use the WithOptions method." )]
        IEventInvoker With( InvokerOptions options );

        [Obsolete( "Use the WithObject and WithOptions methods." )]
        IEventInvoker With( dynamic? target, InvokerOptions options = default );
    }
}