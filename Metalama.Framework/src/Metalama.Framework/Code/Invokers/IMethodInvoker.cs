// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
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
        /// Generates run-time code that invokes the current method with a given list of arguments.
        /// </summary>
        /// <param name="args">A list of C# expressions to be passed to the method. If the compile-time type
        /// of any expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</param>
        /// <returns>A dynamic object representing the return value of the method, if any.</returns>
        /// <remarks>
        /// By default, the method is invoked on the current object (<c>this</c>), unless the method is static. The <c>base</c> implementation 
        /// of the method is invoked, i.e. the implementation <i>before</i> the current aspect layer. To change the default values,
        /// or to use the <c>?.</c> null-conditional operator, use the <see cref="WithOptions"/> method.
        /// </remarks>
        dynamic? Invoke( params dynamic?[] args );

        /// <summary>
        /// Generates run-time code that invokes the current method with a given list of argument expressions. 
        /// </summary>
        /// <param name="args">A list of C# expressions to be passed to the method.</param>
        /// <returns>A dynamic object representing the return value of the method, if any.</returns> 
        /// <remarks>
        /// By default, the method is invoked on the current object (<c>this</c>), unless the method is static. The <c>base</c> implementation 
        /// of the method is invoked, i.e. the implementation <i>before</i> the current aspect layer. To change the default values,
        /// or to use the <c>?.</c> null-conditional operator, use the <see cref="WithOptions"/> method.
        /// </remarks>
        dynamic? Invoke( IEnumerable<IExpression> args );

        /// <summary>
        /// Gets an <see cref="IMethodInvoker"/> for the same method and target with different options.
        /// </summary>
        IMethodInvoker WithOptions( InvokerOptions options );

        /// <summary>
        /// Gets an <see cref="IMethodInvoker"/> for the same method but with a different object, provided as an <see cref="IExpression"/>.
        /// </summary>
        /// <param name="obj">The run-time expression that represents the object for which the method should be called, or <c>null</c> if this is a static method.</param>
        IMethodInvoker WithObject( IExpression? obj );

        /// <summary>
        /// Gets an <see cref="IMethodInvoker"/> for the same method but with a different object, provided as a C# expression.
        /// </summary>
        /// <param name="obj">The run-time expression that represents the object for which the method should be called, or <c>null</c> if this is a static method.
        /// If the compile-time type of the expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</param>
        IMethodInvoker WithObject( dynamic? obj );

        /// <summary>
        /// Creates an <see cref="IExpression"/> that represents the invocation of the method with specific arguments
        /// represented by <see cref="IExpression"/> objects.
        /// </summary>
        /// <param name="args">The list of arguments passed to the method.</param>
        /// <returns>An <see cref="IExpression"/>.</returns>
        /// <remarks>
        /// By default, the method is invoked on the current object (<c>this</c>), unless the method is static. The <c>base</c> implementation 
        /// of the method is invoked, i.e. the implementation <i>before</i> the current aspect layer. To change the default values,
        /// or to use the <c>?.</c> null-conditional operator, use the <see cref="WithOptions"/> method.
        /// </remarks>
        IExpression CreateInvokeExpression( params IEnumerable<IExpression> args );

        /// <summary>
        /// Creates an <see cref="IExpression"/> that represents the invocation of the method with specific arguments
        /// represented by C# expressions.
        /// </summary>
        /// <param name="args">The list of C# expressions passed to the method.  If the compile-time type
        /// of any expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</param>
        /// <returns>An <see cref="IExpression"/>.</returns>
        /// <remarks>
        /// By default, the method is invoked on the current object (<c>this</c>), unless the method is static. The <c>base</c> implementation 
        /// of the method is invoked, i.e. the implementation <i>before</i> the current aspect layer. To change the default values,
        /// or to use the <c>?.</c> null-conditional operator, use the <see cref="WithOptions"/> method.
        /// </remarks>
        IExpression CreateInvokeExpression( params IEnumerable<dynamic?> args );

        [Obsolete( "Use the WithOptions method." )]
        IMethodInvoker With( InvokerOptions options );

        [Obsolete( "Use the WithObject and WithOptions methods." )]
        IMethodInvoker With( dynamic? obj, InvokerOptions options = default );
    }
}