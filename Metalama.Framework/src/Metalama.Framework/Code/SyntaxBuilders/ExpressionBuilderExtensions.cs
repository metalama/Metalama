// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    [CompileTime]
    [PublicAPI]
    public static class ExpressionBuilderExtensions
    {
        /// <summary>
        /// Gets an object that can be used in a run-time expression of a template to represent the result of the current expression builder.
        /// </summary>
        public static dynamic? ToValue( this IExpressionBuilder builder ) => builder.ToExpression().Value;

        /// <summary>
        /// Gets an object that can be used in a run-time expression of a template to represent the result of the current expression builder.
        /// </summary>
        public static dynamic ToValue( this INotNullExpressionBuilder builder ) => builder.ToExpression().Value!;
    }
}