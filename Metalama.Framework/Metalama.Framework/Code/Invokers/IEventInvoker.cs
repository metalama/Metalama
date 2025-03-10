// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

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
        /// use the <see cref="With(Metalama.Framework.Code.Invokers.InvokerOptions)"/> method.
        /// </summary>
        dynamic Add( dynamic? handler );

        /// <summary>
        /// Generates run-time code that removes a given handler from the event. By default, the target instance
        /// of the event is <c>this</c> unless the event is static, and the <c>base</c> implementation of the event is invoked,
        /// i.e. the implementation before the current aspect layer. To change the default values, or to use the <c>?.</c> null-conditional operator,
        /// use the <see cref="With(Metalama.Framework.Code.Invokers.InvokerOptions)"/> method.
        /// </summary>
        dynamic Remove( dynamic? handler );

        /// <summary>
        /// Generates run-time code that raises the current event with specified arguments. By default, the target instance
        /// of the event is <c>this</c> unless the event is static, and the <c>base</c> implementation of the event is invoked,
        /// i.e. the implementation before the current aspect layer. To change the default values, or to use the <c>?.</c> null-conditional operator,
        /// use the <see cref="With(Metalama.Framework.Code.Invokers.InvokerOptions)"/> method.
        /// </summary>
        dynamic? Raise( params dynamic?[] args );

        /// <summary>
        /// Gets an <see cref="IEventInvoker"/> for the same event and target but with different options.
        /// </summary>
        IEventInvoker With( InvokerOptions options );

        /// <summary>
        /// Gets an <see cref="IEventInvoker"/> for the same event but with a different target instance and optionally different options.
        /// </summary>
        /// <param name="target">The run-time expression that represents the target instance of the method. This expression cannot be <c>dynamic</c>.
        /// If you need to pass a <c>dynamic</c> expression, you have to explicitly cast it to <see cref="IExpression"/>.
        /// </param>
        /// <param name="options"></param>
        IEventInvoker With( dynamic? target, InvokerOptions options = default );
    }
}