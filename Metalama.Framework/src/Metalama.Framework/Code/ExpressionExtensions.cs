// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Project;

namespace Metalama.Framework.Code;

/// <summary>
/// Extension methods for <see cref="IExpression"/>.
/// </summary>
[CompileTime]
public static class ExpressionExtensions
{
    /// <summary>
    /// Gets the C# code of the expression.
    /// </summary>
    internal static string ToText( this IExpression expression )
        => MetalamaExecutionContext.CurrentInternal.ExpressionHelper.ConvertExpressionToText( expression );

    /// <summary>
    /// Returns a representation of an expression that is safe to be held across compilations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The counterpart of <see cref="RefExtensions.ToDurableRef{T}"/> for expressions. Call
    /// <see cref="IDurableExpression.ToExpression"/> to get the expression back.
    /// </para>
    /// <para>
    /// The expression is rendered when this method is called, so call it while the expression is still usable.
    /// </para>
    /// </remarks>
    public static IDurableExpression ToDurable( this IExpression expression )
        => MetalamaExecutionContext.CurrentInternal.ExpressionHelper.ToDurableExpression( expression );
}