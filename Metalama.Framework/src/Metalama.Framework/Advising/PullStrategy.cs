// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising.PullStrategies;
using Metalama.Framework.Code;

namespace Metalama.Framework.Advising;

/// <summary>
/// Defines some standard implementations of the <see cref="IPullStrategy"/> interface.
/// </summary>
public static class PullStrategy
{
    /// <summary>
    /// Assigns an expression to the introduced parameter.
    /// </summary>
    /// <param name="expression">An expression. Only a limit set of expressions, including <see cref="TypedConstant"/> and <see cref="IParameter"/>, are supported.</param>
    /// <returns></returns>
    public static IPullStrategy UseExpression( IExpression expression ) => new ExpressionPullStrategy( expression );

    /// <summary>
    /// Assigns a constant to the introduced parameter.
    /// </summary>
    /// <param name="constant"></param>
    /// <returns></returns>
    public static IPullStrategy UseConstant( TypedConstant constant ) => new ExpressionPullStrategy( constant );

    /// <summary>
    /// Introduce a new parameter to the constructor.
    /// </summary>
    public static IPullStrategy IntroduceParameterAndPull(
        string? name = null,
        IType? type = null,
        IExpression? defaultValue = null )
        => new IntroduceParameterPullStrategy( name, type?.ToRef(), defaultValue?.ToText() );
}