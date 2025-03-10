// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
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
    dynamic? Invoke( params dynamic?[] args );

    /// <summary>
    /// Generates run-time code that invokes the current constructor with a given list of argument expressions.
    /// </summary>
    dynamic? Invoke( IEnumerable<IExpression> args );

    IObjectCreationExpression CreateInvokeExpression();

    IObjectCreationExpression CreateInvokeExpression( params dynamic?[] args );

    IObjectCreationExpression CreateInvokeExpression( params IExpression[] args );

    IObjectCreationExpression CreateInvokeExpression( IEnumerable<IExpression> args );
}