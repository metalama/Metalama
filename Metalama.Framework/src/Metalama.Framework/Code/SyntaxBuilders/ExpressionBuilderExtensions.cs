// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// Extension methods for <see cref="IExpressionBuilder"/> and <see cref="INotNullExpressionBuilder"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extension methods provide a convenient way to get a <c>dynamic</c> value from an expression builder,
    /// which can then be used directly in template code. The <see cref="ToValue(IExpressionBuilder)"/> method
    /// calls <see cref="IExpressionBuilder.ToExpression"/> and then accesses <see cref="IExpression.Value"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IExpressionBuilder"/>
    /// <seealso cref="INotNullExpressionBuilder"/>
    /// <seealso cref="IExpression"/>
    /// <seealso href="@run-time-expressions"/>
    /// <seealso href="@templates"/>
    [CompileTime]
    [PublicAPI]
    public static class ExpressionBuilderExtensions
    {
        /// <summary>
        /// Gets an object that can be used in a run-time expression of a template to represent the result of the current expression builder.
        /// </summary>
        /// <param name="builder">The expression builder.</param>
        /// <returns>A dynamic value representing the expression, or <c>null</c> if the expression evaluates to null.</returns>
        public static dynamic? ToValue( this IExpressionBuilder builder ) => builder.ToExpression().Value;

        /// <summary>
        /// Gets an object that can be used in a run-time expression of a template to represent the result of the current expression builder.
        /// </summary>
        /// <param name="builder">The expression builder that produces a non-null value.</param>
        /// <returns>A dynamic value representing the expression.</returns>
        public static dynamic ToValue( this INotNullExpressionBuilder builder ) => builder.ToExpression().Value!;
    }
}