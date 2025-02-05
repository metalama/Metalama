// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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