// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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