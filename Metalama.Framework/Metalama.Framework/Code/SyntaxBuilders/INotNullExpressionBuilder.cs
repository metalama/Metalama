// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// An <see cref="IExpressionBuilder"/> that is guaranteed to produce a non-null value.
    /// </summary>
    public interface INotNullExpressionBuilder : IExpressionBuilder;
}