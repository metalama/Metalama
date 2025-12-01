// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// An <see cref="IExpressionBuilder"/> that is guaranteed to produce a non-null expression value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface is a specialization of <see cref="IExpressionBuilder"/> that indicates the builder will always produce
    /// a non-null result. This allows the <see cref="ExpressionBuilderExtensions.ToValue(INotNullExpressionBuilder)"/> extension
    /// method to return a non-nullable <c>dynamic</c> instead of <c>dynamic?</c>.
    /// </para>
    /// <para>
    /// Builders like <see cref="ArrayBuilder"/> and <see cref="InterpolatedStringBuilder"/> implement this interface because
    /// they always produce valid, non-null expressions (arrays and interpolated strings are never null).
    /// </para>
    /// </remarks>
    /// <seealso cref="IExpressionBuilder"/>
    /// <seealso cref="ExpressionBuilderExtensions"/>
    /// <seealso cref="ArrayBuilder"/>
    /// <seealso cref="InterpolatedStringBuilder"/>
    /// <seealso href="@run-time-expressions"/>
    public interface INotNullExpressionBuilder : IExpressionBuilder;
}