// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// A compile-time representation of a run-time expression, representing C# syntax that will be generated in transformed code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In Metalama templates, <see cref="IExpression"/> objects are compile-time representations of C# syntax that will be generated
    /// in the transformed code. These objects represent the syntax itself, not the evaluation result. For example, <c>1+1</c> and
    /// <c>2</c> are two different expressions even though they evaluate to the same value at run time.
    /// </para>
    /// <para>
    /// <see cref="IExpression"/> objects are two-way convertible with <c>dynamic</c>:
    /// <list type="bullet">
    /// <item>When typed as <see cref="IExpression"/>, expressions can be used in compile-time APIs that manipulate syntax</item>
    /// <item>When accessed via the <see cref="Value"/> property (which returns <c>dynamic</c>), expressions can be used directly in template code as run-time expressions</item>
    /// <item>A <c>dynamic</c> value from a template can be cast back to <see cref="IExpression"/> to use it in compile-time APIs</item>
    /// </list>
    /// </para>
    /// <para>
    /// To create expressions, use <see cref="ExpressionFactory"/> for common scenarios or <see cref="ExpressionBuilder"/> for
    /// programmatic construction. Note that <see cref="IField"/>, <see cref="IProperty"/>, and <see cref="IParameter"/> also
    /// implement <see cref="IExpression"/>, allowing fields and properties to be used directly as expressions.
    /// </para>
    /// </remarks>
    /// <seealso cref="ExpressionFactory"/>
    /// <seealso cref="ExpressionBuilder"/>
    /// <seealso cref="IExpressionBuilder"/>
    /// <seealso cref="TypedConstant"/>
    /// <seealso cref="IHasType"/>
    /// <seealso href="@run-time-expressions"/>
    /// <seealso href="@dynamic-typing"/>
    /// <seealso href="@templates"/>
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