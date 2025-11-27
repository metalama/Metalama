// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Code.SyntaxBuilders;
using System.Collections.Immutable;

namespace Metalama.Framework.Advising;

/// <summary>
/// Represents a way to pull a field or property.
/// </summary>
/// <seealso cref="IPullStrategy"/>
/// <seealso cref="PullActionKind"/>
/// <seealso cref="AdviserExtensions.IntroduceParameter"/>
/// <seealso href="@introducing-constructor-parameters"/>
[CompileTime]
[PublicAPI]
public readonly struct PullAction
{
    internal PullActionKind Kind { get; }

    internal IType? ParameterType { get; }

    internal ImmutableArray<AttributeConstruction> ParameterAttributes { get; }

    internal IExpression? ParameterDefaultValue { get; }

    internal string? ParameterName { get; }

    /// <summary>
    /// Gets the expression to use for pulling the dependency when <see cref="UseExpression"/> or <see cref="UseConstant"/> is used.
    /// </summary>
    public IExpression? Expression { get; }

    private PullAction(
        PullActionKind kind,
        IExpression? expression = null,
        string? parameterName = null,
        IType? parameterType = null,
        IExpression? parameterDefaultValue = null,
        ImmutableArray<AttributeConstruction> parameterAttributes = default )
    {
        this.Kind = kind;
        this.Expression = expression;
        this.ParameterType = parameterType;
        this.ParameterAttributes = parameterAttributes.IsDefault ? ImmutableArray<AttributeConstruction>.Empty : parameterAttributes;
        this.ParameterName = parameterName;
        this.ParameterDefaultValue = parameterDefaultValue;
    }

    /// <summary>
    /// Gets a <see cref="PullAction"/> that means that the dependency has to be set to its default value.
    /// </summary>
    public static PullAction None => new( PullActionKind.DoNotPull );

    /// <summary>
    /// Creates a <see cref="PullAction"/> that means that the dependency should be pulled from an existing constructor parameter.
    /// </summary>
    /// <param name="parameter">The existing parameter to use.</param>
    /// <returns>A <see cref="PullAction"/> that uses the specified parameter.</returns>
    public static PullAction UseExistingParameter( IParameter parameter ) => UseExpression( ExpressionFactory.Parse( parameter.Name ) );

    /// <summary>
    /// Creates a <see cref="PullAction"/> that means that the dependency should be pulled from a new parameter of the calling constructor.
    /// </summary>
    /// <param name="parameterName">Name of the new parameter.</param>
    /// <param name="parameterType">Type of the new parameter.</param>
    /// <param name="parameterDefaultValue">Optional default value for the new parameter.</param>
    /// <param name="parameterAttributes">Optional attributes to apply to the new parameter.</param>
    /// <returns>A <see cref="PullAction"/> that introduces a new parameter and pulls from it.</returns>
    public static PullAction IntroduceParameterAndPull(
        string parameterName,
        IType parameterType,
        IExpression? parameterDefaultValue,
        ImmutableArray<AttributeConstruction> parameterAttributes = default )
        => new( PullActionKind.AppendParameterAndPull, null, parameterName, parameterType, parameterDefaultValue, parameterAttributes );

    /// <summary>
    /// Creates a <see cref="PullAction"/> that means that the dependency should be assigned to a given <see cref="IExpression"/>.
    /// </summary>
    /// <param name="expression">The expression to use for pulling the dependency.</param>
    /// <returns>A <see cref="PullAction"/> that uses the specified expression.</returns>
    public static PullAction UseExpression( IExpression expression ) => new( PullActionKind.UseExpression, expression );

    /// <summary>
    /// Creates a <see cref="PullAction"/> that means that the dependency should be assigned to a given <see cref="TypedConstant"/>.
    /// </summary>
    /// <param name="constant">The constant value to use for pulling the dependency.</param>
    /// <returns>A <see cref="PullAction"/> that uses the specified constant.</returns>
    public static PullAction UseConstant( TypedConstant constant ) => new( PullActionKind.UseExpression, constant );
}