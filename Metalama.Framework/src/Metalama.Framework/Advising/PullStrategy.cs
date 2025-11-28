// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising.PullStrategies;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Advising;

/// <summary>
/// Provides factory methods for creating standard implementations of the <see cref="IPullStrategy"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// A pull strategy determines how an introduced constructor parameter's value is obtained in child constructors.
/// This class provides common implementations that cover most scenarios. For custom behavior, implement
/// <see cref="IPullStrategy"/> directly.
/// </para>
/// <para>
/// The pull strategy is invoked for each constructor that calls the constructor where the parameter was introduced,
/// either through <c>: base(...)</c> or <c>: this(...)</c> syntax. The strategy returns a <see cref="PullAction"/>
/// that specifies what value to pass for the introduced parameter.
/// </para>
/// </remarks>
/// <seealso cref="IPullStrategy"/>
/// <seealso cref="PullAction"/>
/// <seealso cref="AdviserExtensions.IntroduceParameter(IAdviser{IConstructor}, string, IType, TypedConstant, Metalama.Framework.Advising.IPullStrategy?, System.Collections.Immutable.ImmutableArray{Metalama.Framework.Code.DeclarationBuilders.AttributeConstruction})"/>
/// <seealso href="@introducing-constructor-parameters"/>
public static class PullStrategy
{
    /// <summary>
    /// Creates a pull strategy that passes a static expression value to the introduced parameter.
    /// </summary>
    /// <param name="expression">An expression that evaluates to the value to pass. Only static expressions are supported,
    /// such as <see cref="TypedConstant"/>, static field/property access, or static method calls created via <see cref="Code.SyntaxBuilders.ExpressionFactory"/>.
    /// To forward an existing parameter, pass an <see cref="IParameter"/> directly.</param>
    /// <returns>A pull strategy that uses the specified expression to provide the parameter value.</returns>
    /// <remarks>
    /// <para>
    /// This strategy is useful when you want to pass a static or compile-time expression to the introduced parameter.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> While you can pass an <see cref="IParameter"/> directly to this method to forward a parameter value,
    /// it's generally clearer to let <see cref="IntroduceParameterAndPull"/> handle parameter forwarding when appropriate.
    /// </para>
    /// </remarks>
    /// <seealso cref="PullAction.UseExpression"/>
    /// <seealso cref="IntroduceParameterAndPull"/>
    public static IPullStrategy UseExpression( IExpression expression ) => new ExpressionPullStrategy( expression );

    /// <summary>
    /// Creates a pull strategy that passes a constant value to the introduced parameter.
    /// </summary>
    /// <param name="constant">The constant value to pass to the introduced parameter.</param>
    /// <returns>A pull strategy that uses the specified constant to provide the parameter value.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method equivalent to calling <see cref="UseExpression"/> with a <see cref="TypedConstant"/>.
    /// Use this when you want all child constructors to pass the same constant value to the introduced parameter.
    /// </para>
    /// <para>
    /// Example: <c>PullStrategy.UseConstant(TypedConstant.Create(true))</c> will pass <c>true</c> from all child constructors.
    /// </para>
    /// </remarks>
    /// <seealso cref="PullAction.UseConstant"/>
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
    /// This is the most common pull strategy when you want derived classes to provide values for the introduced parameter,
    /// especially for dependency injection scenarios where dependencies should flow through the constructor hierarchy.
    /// </para>
    /// <para>
    /// Example: If you introduce an <c>ILogger</c> parameter to a base class constructor with this strategy, all derived
    /// class constructors will also get an <c>ILogger</c> parameter added, and they will pass it to the base constructor.
    /// </para>
    /// </remarks>
    /// <seealso cref="PullAction.IntroduceParameterAndPull"/>
    public static IPullStrategy IntroduceParameterAndPull(
        string? name = null,
        IType? type = null,
        IExpression? defaultValue = null )
        => new IntroduceParameterPullStrategy( name, type?.ToRef(), defaultValue?.ToText() );
}