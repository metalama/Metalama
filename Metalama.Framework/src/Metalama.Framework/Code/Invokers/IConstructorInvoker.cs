// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Invokers;

/// <summary>
/// Allows generating run-time code that invokes a constructor when you have its compile-time <see cref="IConstructor"/> representation.
/// </summary>
/// <remarks>
/// <para>
/// Invokers are APIs that allow you to generate run-time code from compile-time declarations. When you have an <see cref="IConstructor"/>
/// object (a compile-time representation of a constructor), you can use its invoker methods to create <see cref="IObjectCreationExpression"/>
/// objects that represent object instantiation. These expressions can then be used in templates and will be expanded into actual C# code.
/// </para>
/// <para>
/// The <see cref="Invoke(object?[])"/> method returns a <c>dynamic</c> value that can be used directly in template code, while
/// <see cref="CreateInvokeExpression()"/> and its overloads return an <see cref="IObjectCreationExpression"/> for use in compile-time APIs.
/// </para>
/// <para>
/// Unlike other invokers, constructors do not support <c>WithObject</c> or <c>WithOptions</c> since they always create new instances
/// and do not have implementation layers or nullability concerns in the same way as member access.
/// </para>
/// </remarks>
/// <seealso cref="IConstructor"/>
/// <seealso cref="IExpression"/>
/// <seealso cref="IObjectCreationExpression"/>
/// <seealso href="@invokers"/>
/// <seealso href="@dynamic-typing"/>
[CompileTime]
public interface IConstructorInvoker
{
    /// <summary>
    /// Generates run-time code that invokes the current constructor with a given list of arguments.
    /// </summary>
    /// <param name="args">A list of run-time C# expressions to be passed to the constructor. If the compile-time type
    /// of any expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</param>
    dynamic? Invoke( params dynamic?[] args );

    /// <summary>
    /// Generates run-time code that invokes the current constructor with a given list of argument expressions.
    /// </summary>
    /// <param name="args">The list of compile-time <see cref="IExpression"/> objects passed to the method as arguments.</param>
    /// <remarks>A compile-time dynamic object that represents the method invocation expression.</remarks> 
    dynamic? Invoke( IEnumerable<IExpression> args );

    /// <summary>
    /// Creates an <see cref="IExpression"/> that represents the invocation of constructor without arguments.
    /// </summary>
    /// <returns>An <see cref="IExpression"/>.</returns>
    IObjectCreationExpression CreateInvokeExpression();

    /// <summary>
    /// Creates an <see cref="IExpression"/> that represents the invocation of constructor, where arguments are represented by <see cref="IExpression"/>.
    /// </summary>
    /// <param name="args">The list of compile-time <see cref="IExpression"/> objects passed to the method as arguments.</param>
    /// <returns>An <see cref="IExpression"/> that represents the method invocation.</returns>
    IObjectCreationExpression CreateInvokeExpression( params IEnumerable<IExpression> args );

    /// <summary>
    /// Creates an <see cref="IExpression"/> that represents the invocation of constructor, where arguments are passed as C# expressions.
    /// </summary>
    /// <param name="args">The list of run-time C# expressions passed to the method.  If the compile-time type
    /// of any expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</param>
    /// <returns>An <see cref="IExpression"/> that represents the method invocation.</returns>
    IObjectCreationExpression CreateInvokeExpression( params dynamic?[] args );
}