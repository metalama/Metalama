// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
[CompileTime]
[PublicAPI]
public readonly struct PullAction
{
    internal PullActionKind Kind { get; }

    internal IType? ParameterType { get; }

    internal ImmutableArray<AttributeConstruction> ParameterAttributes { get; }

    internal TypedConstant? ParameterDefaultValue { get; }

    internal string? ParameterName { get; }

    public IExpression? Expression { get; }

    private PullAction(
        PullActionKind kind,
        IExpression? expression = null,
        string? parameterName = null,
        IType? parameterType = null,
        TypedConstant? parameterDefaultValue = null,
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
    public static PullAction UseExistingParameter( IParameter parameter ) => UseExpression( ExpressionFactory.Parse( parameter.Name ) );

    /// <summary>
    /// Creates a <see cref="PullAction"/> that means that the dependency should be pulled from a new parameter of the calling constructor.
    /// </summary>
    /// <param name="parameterName">Name of the new parameter.</param>
    /// <param name="parameterType">Type of the new parameter.</param>
    public static PullAction IntroduceParameterAndPull(
        string parameterName,
        IType parameterType,
        TypedConstant parameterDefaultValue,
        ImmutableArray<AttributeConstruction> parameterAttributes = default )
        => new( PullActionKind.AppendParameterAndPull, null, parameterName, parameterType, parameterDefaultValue, parameterAttributes );

    /// <summary>
    /// Creates a <see cref="PullAction"/> that means that the dependency should be assigned to a given expression.
    /// </summary>
    public static PullAction UseExpression( IExpression expression ) => new( PullActionKind.UseExpression, expression );
}