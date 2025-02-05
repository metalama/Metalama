// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Invokers
{
    /// <summary>
    /// Allows invocation of the method.
    /// </summary>
    [CompileTime]
    public interface IMethodInvoker
    {
        /// <summary>
        /// Generates run-time code that invokes the current method with a given list of arguments. By default, the target instance
        /// of the method is <c>this</c> unless the method is static, and the <c>base</c> implementation of the method is invoked,
        /// i.e. the implementation before the current aspect layer. To change the default values, or to use the <c>?.</c> null-conditional operator,
        /// use the <see cref="With(InvokerOptions)"/> method.
        /// </summary>
        dynamic? Invoke( params dynamic?[] args );

        /// <summary>
        /// Generates run-time code that invokes the current method with a given list of argument expressions. By default, the target instance
        /// of the method is <c>this</c> unless the method is static, and the <c>base</c> implementation of the method is invoked,
        /// i.e. the implementation before the current aspect layer. To change the default values, or to use the <c>?.</c> null-conditional operator,
        /// use the <see cref="With(InvokerOptions)"/> method.
        /// </summary>
        dynamic? Invoke( IEnumerable<IExpression> args );

        /// <summary>
        /// Gets an <see cref="IMethodInvoker"/> for the same method and target with different options.
        /// </summary>
        IMethodInvoker With( InvokerOptions options );

        /// <summary>
        /// Gets an <see cref="IMethodInvoker"/> for the same method but with a different target instance and optionally different options.
        /// </summary>
        /// <param name="target">The run-time expression that represents the target instance of the method. This expression cannot be <c>dynamic</c>.
        /// If you need to pass a <c>dynamic</c> expression, you have to explicitly cast it to <see cref="IExpression"/>.
        /// </param>
        /// <param name="options"></param>
        IMethodInvoker With( dynamic? target, InvokerOptions options = default );

        IMethodInvoker With( IExpression target, InvokerOptions options = default );

        IExpression CreateInvokeExpression( IEnumerable<IExpression> args );
    }
}