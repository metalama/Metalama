// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// Interface to be implemented by any compile-time object that can be serialized into a run-time expression.
    /// </summary>
    [RunTimeOrCompileTime]
    public interface IExpressionBuilder
    {
        /// <summary>
        /// Converts the current object into a run-time expression. 
        /// </summary>
        IExpression ToExpression();
    }
}