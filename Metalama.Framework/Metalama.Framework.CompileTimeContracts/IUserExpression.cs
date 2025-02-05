// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;

namespace Metalama.Framework.CompileTimeContracts
{
    /// <summary>
    /// Adds implementation methods to the public <see cref="IExpression"/> interface. 
    /// </summary>
    public interface IUserExpression : IExpression
    {
        /// <summary>
        /// Creates an <see cref="TypedExpressionSyntax"/> for the current <see cref="IUserExpression"/>.
        /// </summary>
        TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null );
    }
}