// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Invokers;

/// <summary>
/// Allows invocation of the constructor.
/// </summary>
[CompileTime]
public interface IConstructorInvoker
{
    /// <summary>
    /// Generates run-time code that invokes the current constructor with a given list of arguments.
    /// </summary>
    /// <param name="args">A list of C# expressions to be passed to the constructor. If the compile-time type
    /// of any expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</param>
    dynamic? Invoke( params dynamic?[] args );

    /// <summary>
    /// Generates run-time code that invokes the current constructor with a given list of argument expressions.
    /// </summary>
    dynamic? Invoke( IEnumerable<IExpression> args );

    /// <summary>
    /// Creates an <see cref="IExpression"/> that represents the invocation of constructor without arguments.
    /// </summary>
    /// <returns>An <see cref="IExpression"/>.</returns>
    IObjectCreationExpression CreateInvokeExpression();

    /// <summary>
    /// Creates an <see cref="IExpression"/> that represents the invocation of constructor, where arguments are represented by <see cref="IExpression"/>.
    /// </summary>
    /// <param name="args">The list of arguments passed to the method.</param>
    /// <returns>An <see cref="IExpression"/>.</returns>
    IObjectCreationExpression CreateInvokeExpression( params IEnumerable<IExpression> args );

    /// <summary>
    /// Creates an <see cref="IExpression"/> that represents the invocation of constructor, where arguments are passed as C# expressions.
    /// </summary>
    /// <param name="args">The list of C# expressions passed to the method.  If the compile-time type
    /// of any expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</param>
    /// <returns>An <see cref="IExpression"/>.</returns>
    IObjectCreationExpression CreateInvokeExpression( params dynamic?[] args );
}