// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Project;

namespace Metalama.Framework.Code;

/// <summary>
/// Extension methods for <see cref="IExpression"/>.
/// </summary>
internal static class ExpressionExtensions
{
    /// <summary>
    /// Gets the C# code of the expression.
    /// </summary>
    public static string ToText( this IExpression expression )
        => MetalamaExecutionContext.CurrentInternal.ExpressionHelper.ConvertExpressionToText( expression );
}