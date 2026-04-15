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
/// Represents a way to pull a constructor parameter value from child constructors when propagating introduced parameters.
/// </summary>
/// <remarks>
/// <para>
/// When a parameter is introduced to a constructor using <see cref="AdviserExtensions.IntroduceParameter(IAdviser{IConstructor}, string, IType, Metalama.Framework.Advising.IPullStrategy?, ImmutableArray{AttributeConstruction}, Metalama.Framework.Advising.IConstructorOverloadingStrategy?)"/>,
/// child constructors (those that call the modified constructor via <c>: base(...)</c> or <c>: this(...)</c>)
/// need to provide a value for this new parameter. A <see cref="PullAction"/> specifies how to obtain that value.
/// </para>
/// <para>
/// Common scenarios:
/// <list type="bullet">
/// <item><see cref="None"/> - Use the default value of the introduced parameter</item>
/// <item><see cref="UseExistingParameter"/> - Forward an existing parameter from the child constructor</item>
/// <item><see cref="IntroduceParameterAndPull"/> - Add a new parameter to the child constructor and forward its value</item>
/// <item><see cref="UseExpression"/> - Pass a custom expression (e.g., a constant or field access)</item>
/// </list>
/// </para>
/// </remarks>
/// <seealso cref="IPullStrategy"/>
/// <seealso cref="PullStrategy"/>
/// <seealso cref="PullActionKind"/>
/// <seealso cref="AdviserExtensions.IntroduceParameter(IAdviser{IConstructor}, string, IType, Metalama.Framework.Advising.IPullStrategy?, ImmutableArray{AttributeConstruction}, Metalama.Framework.Advising.IConstructorOverloadingStrategy?)"/>
/// <seealso href="@introducing-constructor-parameters"/>
[CompileTime]
[PublicAPI]
public readonly struct PullAction
{
    internal PullActionKind Kind { get; }

    internal IType? ParameterType { get; }

    internal ImmutableArray<AttributeConstruction> ParameterAttributes { get; }

    internal IExpression? ParameterDefaultValue { get; }

    internal IExpression? ForwarderExpression { get; }

    internal string? ParameterName { get; }

    /// <summary>
    /// Gets a value indicating whether, on record targets, the pulled parameter should be appended to the
    /// positional (primary) constructor, making it part of the record's value shape (property, <c>Deconstruct</c>,
    /// <c>Equals</c>, <c>ToString</c>). When <c>false</c> (the default), the parameter is carried on a synthesized
    /// non-primary sibling constructor and the record's positional list is left untouched. Ignored for non-record
    /// targets.
    /// </summary>
    internal bool MaterializeOnRecord { get; }

    /// <summary>
    /// Gets the expression to use for pulling the dependency when <see cref="UseExpression"/> or <see cref="UseConstant"/> is used.
    /// </summary>
    public IExpression? Expression { get; }

    /// <summary>
    /// Gets the existing parameter to replace when <see cref="Kind"/> is <see cref="PullActionKind.ReplaceParameterTypeAndPull"/>.
    /// </summary>
    internal IParameter? ExistingParameter { get; }

    private PullAction(
        PullActionKind kind,
        IExpression? expression = null,
        string? parameterName = null,
        IType? parameterType = null,
        IExpression? parameterDefaultValue = null,
        ImmutableArray<AttributeConstruction> parameterAttributes = default,
        bool materializeOnRecord = false,
        IParameter? existingParameter = null,
        IExpression? forwarderExpression = null )
    {
        this.Kind = kind;
        this.Expression = expression;
        this.ParameterType = parameterType;
        this.ParameterAttributes = parameterAttributes.IsDefault ? ImmutableArray<AttributeConstruction>.Empty : parameterAttributes;
        this.ParameterName = parameterName;
        this.ParameterDefaultValue = parameterDefaultValue;
        this.MaterializeOnRecord = materializeOnRecord;
        this.ExistingParameter = existingParameter;
        this.ForwarderExpression = forwarderExpression;
    }

    /// <summary>
    /// Gets a <see cref="PullAction"/> that means that the dependency has to be set to its default value.
    /// </summary>
    /// <remarks>
    /// When this action is used, the child constructor will pass the default value (as specified in
    /// <see cref="AdviserExtensions.IntroduceParameter(IAdviser{IConstructor}, string, IType, Metalama.Framework.Advising.IPullStrategy?, ImmutableArray{AttributeConstruction}, Metalama.Framework.Advising.IConstructorOverloadingStrategy?)"/>) to the introduced parameter.
    /// </remarks>
    public static PullAction None => new( PullActionKind.DoNotPull );

    /// <summary>
    /// Creates a <see cref="PullAction"/> that means that the dependency should be pulled from an existing constructor parameter.
    /// </summary>
    /// <param name="parameter">The existing parameter to use.</param>
    /// <returns>A <see cref="PullAction"/> that uses the specified parameter.</returns>
    /// <remarks>
    /// This action is useful when the child constructor already has a parameter with the value needed for the
    /// introduced parameter. For example, if you introduce an <c>ILogger</c> parameter to a base constructor,
    /// and a derived class constructor already has an <c>ILogger logger</c> parameter, you can use
    /// <c>UseExistingParameter(loggerParam)</c> to forward that value.
    /// </remarks>
    public static PullAction UseExistingParameter( IParameter parameter ) => UseExpression( ExpressionFactory.Parse( parameter.Name ) );

    /// <summary>
    /// Creates a <see cref="PullAction"/> that means that the dependency should be pulled from a new parameter of the calling constructor.
    /// </summary>
    /// <param name="parameterName">Name of the new parameter.</param>
    /// <param name="parameterType">Type of the new parameter.</param>
    /// <param name="parameterDefaultValue">Optional default value for the new parameter. Must be a compile-time constant
    ///     (or <c>null</c>), because it becomes the declared default of a parameter in the chained constructor.</param>
    /// <param name="forwarderExpression">Optional expression to pass for the introduced parameter in the framework-emitted
    ///     forwarding constructor (the stub that preserves the pre-mutation signature when the required-parameter overload
    ///     is used). Unlike <paramref name="parameterDefaultValue"/>, this expression does not need to be a compile-time
    ///     constant (e.g. <c>System.DateTime.Now</c> is valid). When a forwarder is emitted and this argument is <c>null</c>,
    ///     <paramref name="parameterDefaultValue"/> is used if non-null; otherwise the framework reports a diagnostic.</param>
    /// <param name="parameterAttributes">Optional attributes to apply to the new parameter.</param>
    /// <param name="materializeOnRecord">When <c>true</c> and the target is a record, the parameter is appended to the positional (primary)
    ///     constructor and therefore becomes part of the record's value shape: an auto-generated property, a new entry in
    ///     <c>Equals</c>/<c>GetHashCode</c>/<c>ToString</c>, and a new <c>Deconstruct</c> overload. When <c>false</c> (the default),
    ///     the parameter is carried on a non-primary sibling constructor synthesized by Metalama, and the record's positional list
    ///     is left untouched. Ignored for non-record targets. Set this to <c>true</c> only if you deliberately want the pulled
    ///     parameter to participate in the record's value identity.</param>
    /// <returns>A <see cref="PullAction"/> that introduces a new parameter and pulls from it.</returns>
    /// <remarks>
    /// <para>
    /// This action propagates the parameter requirement to child constructors. A new parameter with the specified
    /// name, type, and default value will be added to each child constructor, and its value will be passed to the
    /// base/this constructor.
    /// </para>
    /// <para>
    /// <strong>Cross-Project Support:</strong> This pull action operates across project boundaries. When a parameter
    /// is introduced to a base class in one project, all derived classes in referencing projects will automatically
    /// have the parameter added to their constructors. This makes it ideal for framework-level dependency injection.
    /// </para>
    /// <para>
    /// This is the most common pull action for dependency injection scenarios where derived classes should also
    /// receive and forward the dependency.
    /// </para>
    /// </remarks>
    public static PullAction IntroduceParameterAndPull(
        string parameterName,
        IType parameterType,
        IExpression? parameterDefaultValue,
        IExpression? forwarderExpression = null,
        ImmutableArray<AttributeConstruction> parameterAttributes = default,
        bool materializeOnRecord = false )
        => new(
            PullActionKind.AppendParameterAndPull,
            expression: null,
            parameterName: parameterName,
            parameterType: parameterType,
            parameterDefaultValue: parameterDefaultValue,
            parameterAttributes: parameterAttributes,
            materializeOnRecord: materializeOnRecord,
            forwarderExpression: forwarderExpression );

    /// <summary>
    /// Creates a <see cref="PullAction"/> that replaces the type of an existing introduced parameter with a more specific type
    /// and continues pulling recursively to further derived constructors.
    /// </summary>
    /// <param name="existingParameter">The existing introduced parameter whose type should be replaced. Must have been introduced by an aspect.</param>
    /// <param name="newParameterType">The new, more specific type for the parameter. Must be convertible to the existing parameter's type.</param>
    /// <param name="parameterDefaultValue">Optional new default value for the parameter.</param>
    /// <returns>A <see cref="PullAction"/> that replaces the parameter type and continues pulling.</returns>
    internal static PullAction ReplaceExistingParameterTypeAndPull(
        IParameter existingParameter,
        IType newParameterType,
        IExpression? parameterDefaultValue = null )
        => new(
            PullActionKind.ReplaceParameterTypeAndPull,
            parameterName: existingParameter.Name,
            parameterType: newParameterType,
            parameterDefaultValue: parameterDefaultValue,
            existingParameter: existingParameter );

    /// <summary>
    /// Creates a <see cref="PullAction"/> that means that the dependency should be assigned to a given <see cref="IExpression"/>.
    /// </summary>
    /// <param name="expression">The expression to use for pulling the dependency. Only static expressions are supported
    /// (e.g., constants, field access, static method calls).</param>
    /// <returns>A <see cref="PullAction"/> that uses the specified expression.</returns>
    /// <remarks>
    /// <para>
    /// This action allows you to pass a static expression to the introduced parameter. Use this for expressions that
    /// can be evaluated at compile-time and are static in nature, i.e. do not reference <c>this</c>.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> To forward an existing parameter from the child constructor, use <see cref="UseExistingParameter"/>
    /// instead, as it provides better readability and type safety.
    /// </para>
    /// </remarks>
    /// <seealso cref="UseExistingParameter"/>
    public static PullAction UseExpression( IExpression expression ) => new( PullActionKind.UseExpression, expression );

    /// <summary>
    /// Creates a <see cref="PullAction"/> that means that the dependency should be assigned to a given <see cref="TypedConstant"/>.
    /// </summary>
    /// <param name="constant">The constant value to use for pulling the dependency.</param>
    /// <returns>A <see cref="PullAction"/> that uses the specified constant.</returns>
    /// <remarks>
    /// This is a convenience method for passing constant values. Use this when all child constructors should
    /// pass the same constant value to the introduced parameter (e.g., <c>UseConstant(TypedConstant.Create(true))</c>).
    /// </remarks>
    public static PullAction UseConstant( TypedConstant constant ) => new( PullActionKind.UseExpression, constant );
}