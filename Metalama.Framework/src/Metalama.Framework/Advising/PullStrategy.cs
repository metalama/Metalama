// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising.PullStrategies;
using Metalama.Framework.Code;

namespace Metalama.Framework.Advising;

/// <summary>
/// Provides factory methods for creating standard implementations of the <see cref="IPullStrategy"/> interface.
/// </summary>
/// <seealso cref="IPullStrategy"/>
/// <seealso cref="PullAction"/>
/// <seealso href="@introducing-constructor-parameters"/>
public static class PullStrategy
{
    /// <summary>
    /// Creates a pull strategy that passes a specific expression value to the introduced parameter.
    /// </summary>
    /// <param name="expression">An expression that evaluates to the value to pass. Only a limited set of expressions are supported,
    /// including <see cref="TypedConstant"/>, <see cref="IParameter"/>, and other static expressions created via <see cref="Code.SyntaxBuilders.ExpressionFactory"/>.</param>
    /// <returns>A pull strategy that uses the specified expression to provide the parameter value.</returns>
    /// <remarks>
    /// This is useful when you want to pass a value from an existing parameter or a specific expression to the introduced parameter.
    /// For example, you can use <c>PullStrategy.UseExpression(existingParameter)</c> to forward an existing parameter's value.
    /// </remarks>
    public static IPullStrategy UseExpression( IExpression expression ) => new ExpressionPullStrategy( expression );

    /// <summary>
    /// Creates a pull strategy that passes a constant value to the introduced parameter.
    /// </summary>
    /// <param name="constant">The constant value to pass to the introduced parameter.</param>
    /// <returns>A pull strategy that uses the specified constant to provide the parameter value.</returns>
    /// <remarks>
    /// This is a convenience method equivalent to calling <see cref="UseExpression"/> with a <see cref="TypedConstant"/>.
    /// Use this when you want all child constructors to pass the same constant value to the introduced parameter.
    /// </remarks>
    public static IPullStrategy UseConstant( TypedConstant constant ) => new ExpressionPullStrategy( constant );

    /// <summary>
    /// Creates a pull strategy that introduces a new parameter in the child constructor and passes its value to the introduced parameter.
    /// </summary>
    /// <param name="name">The name for the new parameter in the child constructor. If <c>null</c>, the introduced parameter's name is used.</param>
    /// <param name="type">The type for the new parameter in the child constructor. If <c>null</c>, the introduced parameter's type is used.</param>
    /// <param name="defaultValue">The default value for the new parameter in the child constructor. If <c>null</c>, no default value is specified.</param>
    /// <returns>A pull strategy that introduces a new parameter in child constructors.</returns>
    /// <remarks>
    /// <para>
    /// This strategy propagates the parameter requirement down the inheritance hierarchy. When a parameter is introduced in a base constructor
    /// using this strategy, each child constructor will also receive a new parameter (with the specified name, type, and default value),
    /// and that parameter's value will be passed to the base constructor.
    /// </para>
    /// <para>
    /// This pull strategy operates across project boundaries. Changes are applied to all types derived from the modified type,
    /// even if those derived types are defined in different projects that reference the project containing the base type.
    /// </para>
    /// <para>
    /// This is the most common pull strategy when you want derived classes to provide values for the introduced parameter.
    /// </para>
    /// </remarks>
    public static IPullStrategy IntroduceParameterAndPull(
        string? name = null,
        IType? type = null,
        IExpression? defaultValue = null )
        => new IntroduceParameterPullStrategy( name, type?.ToRef(), defaultValue?.ToText() );
}