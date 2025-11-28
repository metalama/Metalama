// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Invokers
{
    /// <summary>
    /// Allows generating run-time code that invokes a method when you have its compile-time <see cref="IMethod"/> representation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Invokers are APIs that allow you to generate run-time code from compile-time declarations. When you have an <see cref="IMethod"/>
    /// object (a compile-time representation of a method), you can use its invoker methods to create <see cref="IExpression"/> objects
    /// that represent method calls. These expressions can then be used in templates and will be expanded into actual C# code.
    /// </para>
    /// <para>
    /// The <see cref="Invoke(dynamic[])"/> method returns a <c>dynamic</c> value that can be used directly in template code, while
    /// <see cref="CreateInvokeExpression(IEnumerable{IExpression})"/> returns an <see cref="IExpression"/> for use in compile-time APIs.
    /// Use <see cref="WithObject"/> to specify the target instance and <see cref="WithOptions"/> to control nullability behavior and
    /// which implementation layer (base, current, or final) to invoke.
    /// </para>
    /// <para>
    /// The invoker API is not type-safe. It will generate code even with mismatched argument types, which will only be caught when
    /// the transformed code is compiled. Always verify that generated code matches actual member signatures.
    /// </para>
    /// </remarks>
    /// <seealso cref="IMethod"/>
    /// <seealso cref="InvokerOptions"/>
    /// <seealso cref="IExpression"/>
    /// <seealso href="@invokers"/>
    /// <seealso href="@dynamic-typing"/>
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
        /// <param name="obj">The run-time C# expression that represents the object for which the method should be called, or <c>null</c> if this is a static method.
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