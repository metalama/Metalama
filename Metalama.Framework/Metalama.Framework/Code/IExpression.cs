// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// A compile-time representation of a run-time expression.
    /// To create an expression, use <see cref="ExpressionFactory"/> or <see cref="ExpressionBuilder"/>. Note that
    /// <see cref="IField"/>, <see cref="IProperty"/> and <see cref="IParameter"/> also implement the <see cref="IExpression"/> interface.
    /// </summary>
    [CompileTime]
    [InternalImplement]
    [Hidden]
    public interface IExpression : IHasType
    {
        /// <summary>
        /// Gets a value indicating whether the <see cref="Value"/> can be set.
        /// </summary>
        bool IsAssignable { get; }

        /// <summary>
        /// Gets syntax for the current <see cref="IExpression"/>.
        /// </summary>
        ref dynamic? Value { get; }
    }
}