// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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