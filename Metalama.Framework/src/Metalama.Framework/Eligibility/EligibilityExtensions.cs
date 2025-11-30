// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility.Implementation;
using Metalama.Framework.Project;
using Metalama.Framework.Services;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeInternal

namespace Metalama.Framework.Eligibility;

/// <summary>
/// Extension methods for <see cref="IEligibilityBuilder"/> that provide a fluent API for defining aspect eligibility rules.
/// </summary>
/// <remarks>
/// <para>
/// This class provides the primary API for defining eligibility conditions in the <see cref="IEligible{T}.BuildEligibility"/> method.
/// Methods can be categorized into:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Common rules:</strong> Methods like <see cref="MustNotBeStatic"/>, <see cref="MustNotBeAbstract"/>,
/// and <see cref="MustNotBePartial(Metalama.Framework.Eligibility.IEligibilityBuilder{IMemberOrNamedType})"/> for standard eligibility conditions.</description></item>
/// <item><description><strong>Custom rules:</strong> <see cref="MustSatisfy"/> for defining custom predicates and error messages.</description></item>
/// <item><description><strong>Navigation:</strong> Methods like <see cref="DeclaringType{T}(Metalama.Framework.Eligibility.IEligibilityBuilder{T})"/>, <see cref="ReturnType(Metalama.Framework.Eligibility.IEligibilityBuilder{IMethod})"/>, and <see cref="Parameter(Metalama.Framework.Eligibility.IEligibilityBuilder{IHasParameters}, int)"/>
/// to validate related declarations.</description></item>
/// <item><description><strong>Conditional rules:</strong> <see cref="If"/> to apply rules conditionally, and <see cref="MustSatisfyAny"/>
/// for alternative requirements.</description></item>
/// <item><description><strong>Scenario control:</strong> <see cref="ForScenarios"/> and <see cref="ExceptForScenarios"/>
/// to restrict eligibility to specific usage scenarios.</description></item>
/// </list>
/// </remarks>
/// <seealso href="@eligibility"/>
[CompileTime]
[PublicAPI]
public static partial class EligibilityExtensions
{
    // The order is significant: the most significant should come first.
    private static readonly List<(Type Type, string Name)> _interfaceNames =
    [
        (typeof(IMethod), "method"),
        (typeof(IField), "field"),
        (typeof(INamedType), "type"),
        (typeof(IProperty), "property"),
        (typeof(IEvent), "event"),
        (typeof(IConstructor), "constructor"),
        (typeof(IMethodBase), "method or constructor"),
        (typeof(IParameter), "parameter"),
        (typeof(ICompilation), "compilation"),
        (typeof(INamespace), "namespace"),
        (typeof(ITypeParameter), "type parameter"),
        (typeof(IAttribute), "custom attribute"),
        (typeof(IPropertyOrIndexer), "property or indexer"),
        (typeof(IFieldOrProperty), "field or a property"),
        (typeof(IFieldOrPropertyOrIndexer), "field, property or indexer"),
        (typeof(IHasAccessors), "field, property, indexer or event"),
        (typeof(IHasParameters), "property, indexer or event"),
        (typeof(IMember), "method, constructor, field, property, indexer or event"),
        (typeof(IMemberOrNamedType), "method, constructor, field, property, indexer, event or type")
    ];

    /// <summary>
    /// Gets an <see cref="IEligibilityBuilder"/> for the declaring type of the member validated by the given <see cref="IEligibilityBuilder"/>.
    /// </summary>
    /// <typeparam name="T">The type of member or named type being validated.</typeparam>
    /// <param name="eligibilityBuilder">The parent eligibility builder.</param>
    /// <returns>An eligibility builder for the declaring type.</returns>
    /// <remarks>
    /// <para>
    /// Use this method to validate the type that contains a member. This is useful when an aspect on a member
    /// has requirements for the declaring type (e.g., the type must be static, or must not be an interface).
    /// </para>
    /// <para>
    /// When validation fails on the declaring type, the error message will clearly indicate that it's the
    /// declaring type that doesn't meet the requirement, not the member itself.
    /// </para>
    /// </remarks>
    public static IEligibilityBuilder<INamedType> DeclaringType<T>( this IEligibilityBuilder<T> eligibilityBuilder )
        where T : class, IMemberOrNamedType
        => new ChildEligibilityBuilder<T, INamedType>(
            eligibilityBuilder,
            declaration => declaration as INamedType ?? declaration.DeclaringType!,
            declarationDescription => $"the declaring type '{declarationDescription.Object.DeclaringType}'" );

    /// <summary>
    /// Gets an <see cref="IEligibilityBuilder"/> for the declaring method, property, indexer, or event of the parameter
    /// validated by the given <see cref="IEligibilityBuilder"/>.
    /// </summary>
    /// <param name="eligibilityBuilder">The parent eligibility builder for a parameter.</param>
    /// <returns>An eligibility builder for the declaring member (method, property, indexer, or event) that contains the parameter.</returns>
    /// <remarks>
    /// <para>
    /// Use this method to add requirements on the member that declares a parameter. For example, you can require
    /// that a parameter's declaring method is not static, or that it has specific attributes.
    /// </para>
    /// <para>
    /// When validation fails on the declaring member, the error message will clearly indicate that it's the
    /// declaring member that doesn't meet the requirement.
    /// </para>
    /// </remarks>
    public static IEligibilityBuilder<IHasParameters> DeclaringMember( this IEligibilityBuilder<IParameter> eligibilityBuilder )
    {
        eligibilityBuilder.MustNotBeExtensionBlockReceiverParameter();

        return new ChildEligibilityBuilder<IParameter, IHasParameters>(
            eligibilityBuilder,
            parameter => parameter.DeclaringMember!,
            description => $"the parent member '{description.Object.DeclaringMember}'" );
    }

    /// <summary>
    /// Gets a converter object that allows transforming the given <see cref="IEligibilityBuilder"/> into an
    /// <see cref="IEligibilityBuilder"/> for a more specific declaration type.
    /// </summary>
    /// <typeparam name="T">The type of declaration being validated.</typeparam>
    /// <param name="eligibilityBuilder">The eligibility builder to convert.</param>
    /// <returns>A <see cref="Converter{T}"/> object that provides methods like <c>To&lt;TTarget&gt;()</c> and
    /// <c>When&lt;TTarget&gt;()</c> for type conversion.</returns>
    /// <remarks>
    /// <para>
    /// Use this method when you need to add eligibility rules for a more specific type. For example, if you have
    /// an <see cref="IEligibilityBuilder{T}"/> for <see cref="IMember"/> and want to add rules specific to
    /// <see cref="IMethod"/>, use <c>builder.Convert().To&lt;IMethod&gt;()</c>.
    /// </para>
    /// <para>
    /// The <c>To&lt;TTarget&gt;()</c> method adds an implicit rule that the declaration must be of type <c>TTarget</c>,
    /// while <c>When&lt;TTarget&gt;()</c> only evaluates rules when the declaration is of that type without adding
    /// an eligibility requirement.
    /// </para>
    /// </remarks>
    public static Converter<T> Convert<T>( this IEligibilityBuilder<T> eligibilityBuilder )
        where T : class
        => new( eligibilityBuilder );

    /// <summary>
    /// Gets an <see cref="IEligibilityBuilder"/> for the return type of the method validated by the given <see cref="IEligibilityBuilder"/>.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a method.</param>
    /// <returns>An eligibility builder for the return type of the method.</returns>
    /// <remarks>
    /// <para>
    /// Use this method to add requirements on the return type of a method. For example, you can require that
    /// the return type is not <c>void</c>, or that it implements a specific interface.
    /// </para>
    /// <para>
    /// When validation fails on the return type, the error message will clearly indicate that it's the return type
    /// that doesn't meet the requirement.
    /// </para>
    /// </remarks>
    public static IEligibilityBuilder<IType> ReturnType( this IEligibilityBuilder<IMethod> eligibilityBuilder )
        => new ChildEligibilityBuilder<IMethod, IType>(
            eligibilityBuilder,
            declaration => declaration.ReturnType,
            declarationDescription => $"the return type of {declarationDescription}" );

    /// <summary>
    /// Gets an <see cref="IEligibilityBuilder"/> for the return parameter of the method validated by the given <see cref="IEligibilityBuilder"/>.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a method.</param>
    /// <returns>An eligibility builder for the return parameter of the method.</returns>
    /// <remarks>
    /// <para>
    /// Use this method to add requirements on the return parameter itself (such as custom attributes on the return value).
    /// This is different from <see cref="ReturnType"/> which validates the return type. Use <see cref="ReturnParameter"/>
    /// when you need to check attributes or other characteristics of the return parameter declaration.
    /// </para>
    /// </remarks>
    /// <seealso cref="ReturnType"/>
    public static IEligibilityBuilder<IParameter> ReturnParameter( this IEligibilityBuilder<IMethod> eligibilityBuilder )
        => new ChildEligibilityBuilder<IMethod, IParameter>(
            eligibilityBuilder,
            method => method.ReturnParameter,
            m => $"the return parameter of {m}" );

    /// <summary>
    /// Gets an <see cref="IEligibilityBuilder"/> for a parameter of the method, property, indexer, or event
    /// validated by the given <see cref="IEligibilityBuilder"/>, identified by its zero-based index.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a method, property, indexer or event.</param>
    /// <param name="index">The zero-based index of the parameter to validate.</param>
    /// <returns>An eligibility builder for the specified parameter.</returns>
    /// <remarks>
    /// <para>
    /// Use this method to add requirements on a specific parameter by its position. For example, you can require
    /// that the first parameter is of a certain type or has specific characteristics.
    /// </para>
    /// <para>
    /// When validation fails, the error message will indicate which parameter (by position) doesn't meet the requirement.
    /// </para>
    /// </remarks>
    public static IEligibilityBuilder<IParameter> Parameter( this IEligibilityBuilder<IHasParameters> eligibilityBuilder, int index )
        => new ChildEligibilityBuilder<IHasParameters, IParameter>(
            eligibilityBuilder,
            declaration => declaration.Parameters[index],
            method => $"the {index + 1}-th parameter of {method}",
            method => index < method.Parameters.Count,
            method => $"{method} has fewer than {index + 1} parameter(s)" );

    /// <summary>
    /// Gets an <see cref="IEligibilityBuilder"/> for a parameter of the method, property, indexer, or event
    /// validated by the given <see cref="IEligibilityBuilder"/>, identified by its name.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a method, property, indexer or event.</param>
    /// <param name="name">The name of the parameter to validate.</param>
    /// <returns>An eligibility builder for the specified parameter.</returns>
    /// <remarks>
    /// <para>
    /// Use this method to add requirements on a specific parameter by its name. This is useful when you need to
    /// validate a parameter with a specific name exists and meets certain criteria.
    /// </para>
    /// <para>
    /// When validation fails, the error message will indicate which parameter (by name) doesn't meet the requirement,
    /// or that no parameter with that name exists.
    /// </para>
    /// </remarks>
    public static IEligibilityBuilder<IParameter> Parameter( this IEligibilityBuilder<IHasParameters> eligibilityBuilder, string name )
        => new ChildEligibilityBuilder<IHasParameters, IParameter>(
            eligibilityBuilder,
            declaration => declaration.Parameters[name],
            method => $"parameter '{name}' of {method}",
            method => method.Parameters.All( p => p.Name != name ),
            method => $"{method} has no parameter named '{name}'" );

    /// <summary>
    /// Gets an <see cref="IEligibilityBuilder"/> for the type of the declaration validated by the given <see cref="IEligibilityBuilder"/>.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a declaration that has a type (field, property, parameter, etc.).</param>
    /// <returns>An eligibility builder for the type of the declaration.</returns>
    /// <remarks>
    /// <para>
    /// Use this method to add requirements on the type of a field, property, or parameter. For example, you can require
    /// that a field is of a specific type, or implements a specific interface.
    /// </para>
    /// <para>
    /// When validation fails on the type, the error message will clearly indicate that it's the type
    /// that doesn't meet the requirement.
    /// </para>
    /// </remarks>
    public static IEligibilityBuilder<IType> Type( this IEligibilityBuilder<IHasType> eligibilityBuilder )
        => new ChildEligibilityBuilder<IHasType, IType>(
            eligibilityBuilder,
            declaration => declaration.Type,
            declaration => $"the type of {declaration}",
            _ => true,
            _ => $"" );

    /// <summary>
    /// Gets an <see cref="IEligibilityBuilder"/> for the same declaration as the given <see cref="IEligibilityBuilder"/>
    /// but that is applicable only to specified <see cref="EligibleScenarios"/>.
    /// </summary>
    /// <typeparam name="T">The type of declaration being validated.</typeparam>
    /// <param name="eligibilityBuilder">The parent eligibility builder.</param>
    /// <param name="excludedScenarios">The scenarios for which the following rules should apply. This is a flags enumeration
    /// and can be a combination of scenarios (e.g., <c>EligibleScenarios.Default | EligibleScenarios.Inheritance</c>).</param>
    /// <returns>An eligibility builder that applies only to the specified scenarios.</returns>
    /// <remarks>
    /// <para>
    /// Use this method to restrict eligibility rules to specific scenarios. Rules added to the returned builder
    /// will only be evaluated for the specified scenarios. This is useful when you want different eligibility
    /// requirements for different scenarios.
    /// </para>
    /// <para>
    /// This is the opposite of <see cref="ExceptForScenarios{T}"/> which excludes specific scenarios.
    /// </para>
    /// </remarks>
    /// <seealso cref="EligibleScenarios"/>
    /// <seealso cref="ExceptForScenarios{T}"/>
    public static IEligibilityBuilder<T> ForScenarios<T>( this IEligibilityBuilder<T> eligibilityBuilder, EligibleScenarios excludedScenarios )
        where T : class
        => new ExcludedScenarioEligibilityBuilder<T>( eligibilityBuilder, EligibleScenarios.All & ~excludedScenarios );

    /// <summary>
    /// Gets an <see cref="IEligibilityBuilder"/> for the same declaration as the given <see cref="IEligibilityBuilder"/>
    /// but that is not applicable to specified <see cref="EligibleScenarios"/>.
    /// </summary>
    /// <typeparam name="T">The type of declaration being validated.</typeparam>
    /// <param name="eligibilityBuilder">The parent eligibility builder.</param>
    /// <param name="excludedScenarios">The scenarios to exclude from eligibility. This is a flags enumeration
    /// and can be a combination of scenarios (e.g., <c>EligibleScenarios.Inheritance | EligibleScenarios.LiveTemplate</c>).</param>
    /// <returns>An eligibility builder that excludes the specified scenarios.</returns>
    /// <remarks>
    /// <para>
    /// Use this method to exclude specific scenarios from eligibility. Rules added to the returned builder
    /// will mark the declaration as ineligible for the specified scenarios. This is useful when you want to
    /// prevent aspects from being applied in certain contexts (e.g., excluding inheritance scenarios).
    /// </para>
    /// <para>
    /// This is the opposite of <see cref="ForScenarios{T}"/> which restricts to specific scenarios only.
    /// </para>
    /// </remarks>
    /// <seealso cref="EligibleScenarios"/>
    /// <seealso cref="ForScenarios{T}"/>
    /// <seealso cref="ExceptForInheritance{T}"/>
    public static IEligibilityBuilder<T> ExceptForScenarios<T>( this IEligibilityBuilder<T> eligibilityBuilder, EligibleScenarios excludedScenarios )
        where T : class
        => new ExcludedScenarioEligibilityBuilder<T>( eligibilityBuilder, excludedScenarios );

    /// <summary>
    /// Gets an <see cref="IEligibilityBuilder"/> for the same declaration as the given <see cref="IEligibilityBuilder"/>
    /// but that is not applicable when the aspect is inheritable and is applied to a declaration that can be inherited or overridden.
    /// </summary>
    /// <typeparam name="T">The type of declaration being validated.</typeparam>
    /// <param name="eligibilityBuilder">The parent eligibility builder.</param>
    /// <returns>An eligibility builder that excludes the <see cref="EligibleScenarios.Inheritance"/> scenario.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method equivalent to calling <c>ExceptForScenarios(EligibleScenarios.Inheritance)</c>.
    /// Use it to prevent aspects from being eligible on derived or overridden declarations when the aspect
    /// is marked with <see cref="InheritableAttribute"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="ExceptForScenarios{T}"/>
    /// <seealso cref="EligibleScenarios.Inheritance"/>
    /// <seealso href="@aspect-inheritance"/>
    public static IEligibilityBuilder<T> ExceptForInheritance<T>( this IEligibilityBuilder<T> eligibilityBuilder )
        where T : class
        => new ExcludedScenarioEligibilityBuilder<T>( eligibilityBuilder, EligibleScenarios.Inheritance );

    /// <summary>
    /// Adds a group of conditions to the given <see cref="IEligibilityBuilder"/>, where at least one condition must be satisfied
    /// by the declaration in order to be eligible for the aspect.
    /// </summary>
    /// <typeparam name="T">The type of declaration being validated.</typeparam>
    /// <param name="eligibilityBuilder">The parent eligibility builder.</param>
    /// <param name="requirements">A variable number of actions that define alternative eligibility rules.
    /// The declaration is eligible if at least one of these rules is satisfied.</param>
    /// <remarks>
    /// <para>
    /// Use this method when a declaration should be eligible if it meets any of several alternative criteria.
    /// For example, a method could be eligible if it's either static OR has a specific attribute.
    /// </para>
    /// <para>
    /// This implements an OR logic between the specified requirements. See <see cref="MustSatisfyAll{T}"/> for AND logic.
    /// </para>
    /// </remarks>
    /// <seealso cref="MustSatisfyAll{T}"/>
    public static void MustSatisfyAny<T>( this IEligibilityBuilder<T> eligibilityBuilder, params Action<IEligibilityBuilder<T>>[] requirements )
        where T : class
        => eligibilityBuilder.Aggregate( BooleanCombinationOperator.Or, requirements );

    /// <summary>
    /// Adds a group of conditions to the given <see cref="IEligibilityBuilder"/>, where all conditions must be satisfied
    /// by the declaration in order to be eligible for the aspect.
    /// </summary>
    /// <typeparam name="T">The type of declaration being validated.</typeparam>
    /// <param name="eligibilityBuilder">The parent eligibility builder.</param>
    /// <param name="requirements">A variable number of actions that define eligibility rules.
    /// The declaration is eligible only if all of these rules are satisfied.</param>
    /// <remarks>
    /// <para>
    /// Use this method to group multiple eligibility requirements that must all be met. This is typically
    /// used for organizational purposes when you have multiple related conditions.
    /// </para>
    /// <para>
    /// This implements an AND logic between the specified requirements. Note that adding requirements directly
    /// to the builder also uses AND logic, so this method is primarily useful for grouping. See <see cref="MustSatisfyAny{T}"/>
    /// for OR logic.
    /// </para>
    /// </remarks>
    /// <seealso cref="MustSatisfyAny{T}"/>
    public static void MustSatisfyAll<T>( this IEligibilityBuilder<T> eligibilityBuilder, params Action<IEligibilityBuilder<T>>[] requirements )
        where T : class
        => eligibilityBuilder.Aggregate( BooleanCombinationOperator.And, requirements );

    /// <summary>
    /// Returns an <see cref="IEligibilityBuilder"/> that conditionally applies rules based on a predicate.
    /// Rules added to the returned builder are only evaluated if the condition is satisfied.
    /// </summary>
    /// <typeparam name="T">The type of declaration being validated.</typeparam>
    /// <param name="eligibilityBuilder">The parent eligibility builder.</param>
    /// <param name="condition">A predicate that determines whether the following rules should be evaluated.
    /// If this returns <c>false</c>, the rules are ignored and the declaration is considered eligible (for those rules).</param>
    /// <returns>An eligibility builder where rules are only evaluated when the condition is true.</returns>
    /// <remarks>
    /// <para>
    /// Use this method when eligibility depends on a condition. For example, you might require static methods to have
    /// no parameters, but allow instance methods to have parameters. The rules added after <c>If</c> only apply
    /// when the condition is met.
    /// </para>
    /// <para>
    /// This is conceptually similar to: "If the declaration is X, then it must also satisfy Y."
    /// If the declaration is not X, the Y requirement is ignored.
    /// </para>
    /// </remarks>
    public static IEligibilityBuilder<T> If<T>( this IEligibilityBuilder<T> eligibilityBuilder, Predicate<T> condition )
        where T : class
        => new ConditionalEligibilityBuilder<T>( eligibilityBuilder, condition );

    /// <summary>
    /// Adds a custom eligibility condition to the given <see cref="IEligibilityBuilder"/>, where the condition must be
    /// satisfied by the declaration in order to be eligible for the aspect.
    /// </summary>
    /// <param name="eligibilityBuilder">The parent <see cref="IEligibilityBuilder"/>.</param>
    /// <param name="predicate">A predicate that returns <c>true</c> if the declaration is eligible for the aspect.</param>
    /// <param name="getJustification">A delegate called when the <paramref name="predicate"/> returns <c>false</c> and an
    /// error message is needed. This delegate must return a <see cref="FormattableString"/> (a C# interpolated string like <c>$"like {this}"</c>)
    /// explaining what the declaration <em>must</em> be to satisfy the condition.</param>
    /// <remarks>
    /// <para>
    /// Use this method when standard eligibility methods like <see cref="MustNotBeStatic"/> or <see cref="MustNotBeAbstract"/>
    /// are insufficient for your requirements. The <paramref name="getJustification"/> message should state what the declaration
    /// <em>must</em> be (e.g., "must not be a record type") rather than what it <em>must not</em> be, as this convention
    /// combines better when multiple conditions are violated.
    /// </para>
    /// <para>
    /// Do not format the string yourself—use a raw interpolated string. The framework uses a custom formatter
    /// to ensure proper rendering of declarations.
    /// </para>
    /// </remarks>
    public static void MustSatisfy<T>(
        this IEligibilityBuilder<T> eligibilityBuilder,
        Predicate<T> predicate,
        Func<IDescribedObject<T>, FormattableString> getJustification )
        where T : class
        => eligibilityBuilder.AddRule( new EligibilityRule<T>( eligibilityBuilder.IneligibleScenarios, predicate, getJustification ) );

    /// <summary>
    /// Adds rules to the given <see cref="IEligibilityBuilder"/> by invoking an action that operates directly on the builder.
    /// </summary>
    /// <typeparam name="T">The type of declaration being validated.</typeparam>
    /// <param name="eligibilityBuilder">The parent eligibility builder.</param>
    /// <param name="requirement">An action that adds one or more eligibility rules to the builder.</param>
    /// <remarks>
    /// <para>
    /// This is a helper method that allows grouping multiple eligibility rules within an action delegate.
    /// It's functionally equivalent to calling the action directly on the builder, but can improve code organization.
    /// </para>
    /// </remarks>
    public static void AddRules<T>( this IEligibilityBuilder<T> eligibilityBuilder, Action<IEligibilityBuilder<T>> requirement )
        where T : class
        => requirement( eligibilityBuilder );

    /// <summary>
    /// Requires the target method not to have <c>ref</c> or <c>out</c> parameters.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a method.</param>
    /// <remarks>
    /// This is commonly used for aspects that cannot handle by-reference parameters, such as caching or logging aspects.
    /// </remarks>
    public static void MustNotHaveRefOrOutParameter( this IEligibilityBuilder<IMethod> eligibilityBuilder )
        => eligibilityBuilder.AddRule(
            new EligibilityRule<IMethod>(
                eligibilityBuilder.IneligibleScenarios,
                m => !m.Parameters.Any( p => p.RefKind is RefKind.Out or RefKind.Ref ),
                d => $"{d} cannot have any ref or out parameter" ) );

    /// <summary>
    /// Requires the target member or type to have exactly one of the specified accessibilities.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a member or named type.</param>
    /// <param name="accessibility">The first allowed accessibility level.</param>
    /// <param name="otherAccessibilities">Additional allowed accessibility levels.</param>
    /// <remarks>
    /// Use this method when an aspect should only apply to members with specific accessibility levels, such as
    /// only public and internal members, or only private members.
    /// </remarks>
    public static void MustHaveAccessibility(
        this IEligibilityBuilder<IMemberOrNamedType> eligibilityBuilder,
        Accessibility accessibility,
        params Accessibility[] otherAccessibilities )
        => eligibilityBuilder.MustSatisfy(
            member => member.Accessibility == accessibility || otherAccessibilities.Contains( member.Accessibility ),
            member =>
            {
                var accessibilities = new[] { accessibility }.Concat( otherAccessibilities ).ToArray();

                var formattedAccessibilities = string.Join(
                    " or ",
                    accessibilities.Select( a => string.Format( MetalamaExecutionContext.Current.FormatProvider, "{0}", a ) ) );

                return $"{member} must be {formattedAccessibilities}";
            } );

    /// <summary>
    /// Requires the target method not to be partial.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a method.</param>
    public static void MustNotBePartial( this IEligibilityBuilder<IMethod> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            m => !m.IsPartial,
            method => $"{method} must not be partial" );

    /// <summary>
    /// Requires the target declaration not to be partial.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a member or named type.</param>
    public static void MustNotBePartial( this IEligibilityBuilder<IMemberOrNamedType> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            d => !d.IsPartial,
            declaration => $"{declaration} must not be partial" );

    /// <summary>
    /// Requires the target field, property, or indexer to be writable.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a field, property, or indexer.</param>
    public static void MustBeWritable( this IEligibilityBuilder<IFieldOrPropertyOrIndexer> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            member => member.Writeability != Writeability.None,
            member => $"{member} must be writable" );

    /// <summary>
    /// Requires the target field, property, or indexer to be readable.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a field, property, or indexer.</param>
    public static void MustBeReadable( this IEligibilityBuilder<IFieldOrPropertyOrIndexer> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfyAny(
            b => b.MustBeInstanceOfType( typeof(IField) ),
            b => b.Convert().To<IPropertyOrIndexer>().MustSatisfy( d => d.GetMethod != null, d => $"{d} must have a getter" ) );

    /// <summary>
    /// Requires the target parameter to be writable, i.e. <c>ref</c> or <c>out</c>.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a parameter.</param>
    public static void MustBeWritable( this IEligibilityBuilder<IParameter> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            p => p.RefKind is RefKind.Ref or RefKind.Out,
            member => $"{member} must be an 'out' or 'ref' parameter" );

    /// <summary>
    /// Requires the target parameter to be readable, i.e. not <c>out</c>.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a parameter.</param>
    public static void MustBeReadable( this IEligibilityBuilder<IParameter> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            p => p.RefKind != RefKind.Out,
            member => $"{member} must not be an 'out' parameter" );

    /// <summary>
    /// Requires the target parameter to be the return parameter.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a parameter.</param>
    public static void MustBeReturnParameter( this IEligibilityBuilder<IParameter> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            p => p.IsReturnParameter,
            member => $"{member} must be the return value parameter" );

    /// <summary>
    /// Forbids the target parameter from being the return parameter.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a parameter.</param>
    public static void MustNotBeReturnParameter( this IEligibilityBuilder<IParameter> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            p => !p.IsReturnParameter,
            member => $"{member} must not be the return value parameter" );

    /// <summary>
    /// Forbids the target parameter from being the receiver parameter of an extension block.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a parameter.</param>
    public static void MustNotBeExtensionBlockReceiverParameter( this IEligibilityBuilder<IParameter> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            p => p.ContainingDeclaration is not IExtensionBlock,
            member => $"{member} must not be the receiver parameter of an extension block" );

    /// <summary>
    /// Requires the target parameter to be <c>ref</c>.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a parameter.</param>
    public static void MustBeRef( this IEligibilityBuilder<IParameter> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            p => p.RefKind == RefKind.Ref,
            member => $"{member} must be a 'ref' parameter" );

    /// <summary>
    /// Requires the target parameter not to be <c>void</c>.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a parameter.</param>
    public static void MustNotBeVoid( this IEligibilityBuilder<IParameter> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            p => !p.Type.Equals( SpecialType.Void ),
            member => $"{member} must not be void" );

    /// <summary>
    /// Requires the target declaration to be explicitly declared in source code (not compiler-generated).
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a declaration.</param>
    public static void MustBeExplicitlyDeclared( this IEligibilityBuilder<IDeclaration> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            m => !m.IsImplicitlyDeclared,
            m => $"{m} must be explicitly declared" );

    /// <summary>
    /// Forbids the target field, property, or indexer from being <c>ref</c> or <c>ref readonly</c>.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a field, property, or indexer.</param>
    public static void MustNotBeRef( this IEligibilityBuilder<IFieldOrPropertyOrIndexer> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy( f => f.RefKind == RefKind.None, f => $"{f} must not be 'ref'" );

    /// <summary>
    /// Forbids the target type from being a <c>ref struct</c>.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a named type.</param>
    public static void MustNotBeRef( this IEligibilityBuilder<INamedType> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy( f => !f.IsRef, f => $"{f} must not be a 'ref struct'" );

    /// <summary>
    /// Forbids the target method from returning a <c>ref</c>.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a method.</param>
    public static void MustNotBeRef( this IEligibilityBuilder<IMethod> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy( f => f.ReturnParameter.RefKind == RefKind.None, f => $"{f} must not be a 'ref' method" );

    private static string GetInterfaceName<T>() => GetInterfaceName( typeof(T) );

    private static string GetInterfaceName( Type type )
    {
        if ( type is ICompileTimeType )
        {
            return type.Name;
        }

        foreach ( var pair in _interfaceNames )
        {
            if ( pair.Type.IsAssignableFrom( type ) )
            {
                return pair.Name;
            }
        }

        return type.Name;
    }

    [Obsolete( "This method has been renamed IsInstanceOfType." )]
    public static void MustBeOfType<T>( this IEligibilityBuilder<T> eligibilityBuilder, Type type )
        where T : class
        => MustBeInstanceOfType( eligibilityBuilder, type );

    /// <summary>
    /// Requires the validated object to be of a certain type of metadata object, e.g. an <see cref="IField"/> or <see cref="IMethod"/>.
    /// To check the type of a field, property or parameter, use code like <c>builder.Type().MustBeConvertibleTo(typeof(string));</c> instead.
    /// </summary>
    /// <typeparam name="T">The type of declaration being validated.</typeparam>
    /// <param name="eligibilityBuilder">The eligibility builder.</param>
    /// <param name="type">The metadata type that the declaration must be an instance of (e.g., <c>typeof(IMethod)</c>, <c>typeof(IProperty)</c>).</param>
    /// <remarks>
    /// <para>Note that this validates the object itself, not the declaration that it represents.
    /// For instance, if the object is an <see cref="IParameter"/> and the <paramref name="type"/> parameter is set to <c>typeof(string)</c>,
    /// this method will fail with an exception, because no conversion exists from <see cref="IParameter"/> to <c>string</c>.</para>
    /// <para>On the other hand, code like <c>builder.MustBeInstanceOfType(typeof(IProperty));</c> will correctly check that a declaration is a property.</para>
    /// </remarks>
    public static void MustBeInstanceOfType<T>( this IEligibilityBuilder<T> eligibilityBuilder, Type type )
        where T : class
    {
        if ( !typeof(T).IsAssignableFrom( type ) )
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                $"An object of type '{typeof(T)}' can never be converted to the type '{type}'. To check the type of a declaration, use code like `builder.Type().MustBe(typeof(string));` instead." );
        }

        eligibilityBuilder.MustSatisfy(
            type.IsInstanceOfType,
            d => $"{d} must be a {GetInterfaceName( type )}" );
    }

    [Obsolete( "This method has been renamed MustBeInstanceOfAnyType." )]
    public static void MustBeOfAnyType<T>(
        this IEligibilityBuilder<T> eligibilityBuilder,
        params Type[] types )
        where T : class
        => MustBeInstanceOfAnyType( eligibilityBuilder, types );

    /// <summary>
    /// Requires the validated object to be of one of the specified metadata types. Note that this validates the object itself, not the declaration
    /// that it represents. For instance, if the object is an <see cref="IParameter"/> and the <paramref name="types"/> parameter
    /// contains <c>typeof(string)</c>, this method will fail with an exception because no conversion exists from <see cref="IParameter"/> to <c>string</c>.
    /// </summary>
    /// <typeparam name="T">The type of declaration being validated.</typeparam>
    /// <param name="eligibilityBuilder">The eligibility builder.</param>
    /// <param name="types">The metadata types that the declaration can be an instance of (e.g., <c>typeof(IMethod)</c>, <c>typeof(IProperty)</c>).</param>
    public static void MustBeInstanceOfAnyType<T>(
        this IEligibilityBuilder<T> eligibilityBuilder,
        params Type[] types )
        where T : class
    {
        foreach ( var type in types )
        {
            if ( !typeof(T).IsAssignableFrom( type ) )
            {
                throw new ArgumentOutOfRangeException( nameof(types), $"An object of type '{typeof(T)}' can never be converted to the type '{type}'." );
            }
        }

        eligibilityBuilder.MustSatisfy(
            t => types.Any( i => i.IsInstanceOfType( t ) ),
            member => $"{member} cannot be converted to an {string.Join( " or ", types.Select( GetInterfaceName ) )}" );
    }

    /// <summary>
    /// Requires the target type to be run-time, as opposed to compile-time or run-time-or-compile-time.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a named type.</param>
    /// <seealso cref="CompileTimeAttribute"/>
    /// <seealso cref="RunTimeOrCompileTimeAttribute"/>
    public static void MustBeRunTimeOnly( this IEligibilityBuilder<INamedType> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            member => member.ExecutionScope == ExecutionScope.RunTime,
            member => $"the execution scope of {member} must be run-time but is {member.Object.ExecutionScope}" );

    /// <summary>
    /// Requires the target member or type to be static.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a member or named type.</param>
    public static void MustBeStatic( this IEligibilityBuilder<IMemberOrNamedType> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            member => member.IsStatic,
            member => $"{member} must be static" );

    /// <summary>
    /// Forbids the target member or type from being static.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a member or named type.</param>
    public static void MustNotBeStatic( this IEligibilityBuilder<IMemberOrNamedType> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            member => !member.IsStatic,
            member => $"{member} must not be static" );

    /// <summary>
    /// Forbids the target method from being extern.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a method.</param>
    public static void MustNotBeExtern( this IEligibilityBuilder<IMethod> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            member => !member.IsExtern,
            member => $"{member} must not be extern" );

    /// <summary>
    /// Forbids the target member or type from being abstract.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a member or named type.</param>
    public static void MustNotBeAbstract( this IEligibilityBuilder<IMemberOrNamedType> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            member => !member.IsAbstract,
            member => $"{member} must not be abstract" );

    /// <summary>
    /// Forbids the target constructor from being a primary constructor.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a constructor.</param>
    public static void MustNotBePrimaryConstructor( this IEligibilityBuilder<IConstructor> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            member => member is not { IsPrimary: true },
            member => $"{member} must not be a primary constructor" );

    /// <summary>
    /// Forbids the target constructor from being a primary constructor of a class or a struct (C# 12.0 feature).
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a constructor.</param>
    public static void MustNotBePrimaryConstructorOfNonRecordType( this IEligibilityBuilder<IConstructor> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            member => member is not { IsPrimary: true, DeclaringType.TypeKind: TypeKind.Class or TypeKind.Struct },
            member => $"{member} must not be a primary constructor of a non-record type" );

    /// <summary>
    /// Forbids the target constructor from being the copy constructor of a record.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a constructor.</param>
    public static void MustNotBeRecordCopyConstructor( this IEligibilityBuilder<IConstructor> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            member => !member.IsRecordCopyConstructor(),
            member => $"{member} must not be the copy constructor of a record type" );

    /// <summary>
    /// Forbids the target type from being an interface.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a named type.</param>
    public static void MustNotBeInterface( this IEligibilityBuilder<INamedType> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            member => member.TypeKind != TypeKind.Interface,
            member => $"{member} must not an interface" );

    /// <summary>
    /// Forbids the target member from being declared in an extension block.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a member.</param>
    public static void MustNotBeExtensionMember( this IEligibilityBuilder<IMember> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            member => member.DeclaringType is not { TypeKind: TypeKind.Extension },
            member => $"{member} must not be an extension block member" );

    /// <summary>
    /// Forbids the target type from being an extension block.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a named type.</param>
    public static void MustNotBeExtensionBlock( this IEligibilityBuilder<INamedType> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            member => member is not { TypeKind: TypeKind.Extension },
            member => $"{member} must not be an extension block" );

    [Obsolete( "This method has been renamed MustBeConvertibleTo or MustEqual." )]
    public static void MustBe( this IEligibilityBuilder<IType> eligibilityBuilder, Type type, ConversionKind conversionKind = ConversionKind.Default )
        => MustBeConvertibleTo( eligibilityBuilder, type, conversionKind );

    /// <summary>
    /// Requires the target type to be convertible to a given type (specified as a reflection <see cref="System.Type"/>).
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a type.</param>
    /// <param name="type">The target type that the validated type must be convertible to.</param>
    /// <param name="conversionKind">The kind of conversion required (default, implicit, explicit, or reference). Default is <see cref="ConversionKind.Default"/>.</param>
    public static void MustBeConvertibleTo(
        this IEligibilityBuilder<IType> eligibilityBuilder,
        Type type,
        ConversionKind conversionKind = ConversionKind.Default )
        => eligibilityBuilder.MustSatisfy(
            t => t.IsConvertibleTo( type, conversionKind ),
            t => $"{t} must be convertible to '{type}'" );

    [Obsolete( "This method has been renamed MustBeConvertibleTo or MustEqual." )]
    public static void MustBe( this IEligibilityBuilder<IType> eligibilityBuilder, IType type, ConversionKind conversionKind = ConversionKind.Default )
        => MustBeConvertibleTo( eligibilityBuilder, type, conversionKind );

    /// <summary>
    /// Requires the target type to be convertible to a given type (specified as an <see cref="IType"/>).
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a type.</param>
    /// <param name="type">The target type that the validated type must be convertible to.</param>
    /// <param name="conversionKind">The kind of conversion required (default, implicit, explicit, or reference). Default is <see cref="ConversionKind.Default"/>.</param>
    public static void MustBeConvertibleTo(
        this IEligibilityBuilder<IType> eligibilityBuilder,
        IType type,
        ConversionKind conversionKind = ConversionKind.Default )
        => eligibilityBuilder.MustSatisfy(
            t => t.IsConvertibleTo( type, conversionKind ),
            t => $"{t} must be convertible to '{type}'" );

    [Obsolete( "This method has been renamed MustBeConvertibleTo or MustEqual." )]
    public static void MustBe<T>( this IEligibilityBuilder<IType> eligibilityBuilder, ConversionKind conversionKind = ConversionKind.Default )
        => MustBeConvertibleTo<T>( eligibilityBuilder, conversionKind );

    /// <summary>
    /// Requires the target type to be convertible to a given type (specified as a type parameter).
    /// </summary>
    /// <typeparam name="T">The target type that the validated type must be convertible to.</typeparam>
    /// <param name="eligibilityBuilder">The eligibility builder for a type.</param>
    /// <param name="conversionKind">The kind of conversion required (default, implicit, explicit, or reference). Default is <see cref="ConversionKind.Default"/>.</param>
    public static void MustBeConvertibleTo<T>( this IEligibilityBuilder<IType> eligibilityBuilder, ConversionKind conversionKind = ConversionKind.Default )
        => eligibilityBuilder.MustBeConvertibleTo( typeof(T), conversionKind );

    /// <summary>
    /// Requires the target declaration to equal a specified value.
    /// </summary>
    /// <typeparam name="T">The type of declaration being validated.</typeparam>
    /// <param name="eligibilityBuilder">The eligibility builder.</param>
    /// <param name="other">The value that the declaration must equal.</param>
    public static void MustEqual<T>( this IEligibilityBuilder<T> eligibilityBuilder, T other )
        where T : class, IEquatable<T>
        => eligibilityBuilder.MustSatisfy( t => t.Equals( other ), x => $"{x} must equal '{other}'" );

    /// <summary>
    /// Requires the target type to equal a specified type (specified as a reflection <see cref="System.Type"/>).
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a type.</param>
    /// <param name="otherType">The type that the validated type must equal.</param>
    public static void MustEqual( this IEligibilityBuilder<IType> eligibilityBuilder, Type otherType )
        => eligibilityBuilder.MustSatisfy( t => t.Equals( t.Compilation.Factory.GetTypeByReflectionType( otherType ) ), x => $"{x} must equal '{otherType}'" );

    /// <summary>
    /// Requires the target type to equal a specified special type.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a type.</param>
    /// <param name="otherType">The special type that the validated type must equal.</param>
    public static void MustEqual( this IEligibilityBuilder<IType> eligibilityBuilder, SpecialType otherType )
        => eligibilityBuilder.MustSatisfy( t => t.Equals( t.Compilation.Factory.GetSpecialType( otherType ) ), x => $"{x} must equal '{otherType}'" );

    private static void Aggregate<T>(
        this IEligibilityBuilder<T> eligibilityBuilder,
        BooleanCombinationOperator combinationOperator,
        params Action<IEligibilityBuilder<T>>[] requirements )
        where T : class
    {
        switch ( requirements.Length )
        {
            case 0:
                throw new ArgumentOutOfRangeException( nameof(requirements), "At least one requirement must be provided." );

            case 1:
                requirements[0]( eligibilityBuilder );

                return;

            default:
                var orBuilder = new EligibilityBuilder<T>( combinationOperator );

                foreach ( var requirement in requirements )
                {
                    requirement( orBuilder );
                }

                eligibilityBuilder.AddRule( orBuilder.Build() );

                return;
        }
    }

    /// <summary>
    /// Requires the target declaration to have an aspect of a given type.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a declaration.</param>
    /// <param name="aspectType">The exact aspect type. Derived types are not taken into account.</param>
    /// <remarks>
    /// Use this method when an aspect should only be applied to declarations that already have another specific aspect.
    /// </remarks>
    public static void MustHaveAspectOfType( this IEligibilityBuilder<IDeclaration> eligibilityBuilder, Type aspectType )
        => eligibilityBuilder.MustSatisfy(
            d => d.Enhancements().HasAspect( aspectType ),
            d => $"{d} must have an aspect of type {aspectType.Name}" );

    /// <summary>
    /// Forbids the target declaration from having an aspect of a given type.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a declaration.</param>
    /// <param name="aspectType">The exact aspect type. Derived types are not taken into account.</param>
    /// <remarks>
    /// Use this method to prevent aspect conflicts or ensure mutual exclusivity between aspects.
    /// </remarks>
    public static void MustNotHaveAspectOfType( this IEligibilityBuilder<IDeclaration> eligibilityBuilder, Type aspectType )
        => eligibilityBuilder.MustSatisfy(
            d => !d.Enhancements().HasAspect( aspectType ),
            d => $"{d} must not have an aspect of type {aspectType.Name}" );

    /// <summary>
    /// Requires the target declaration to have an attribute of a given type.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a declaration.</param>
    /// <param name="attributeType">The attribute type to check for.</param>
    public static void MustHaveAttributeOfType( this IEligibilityBuilder<IDeclaration> eligibilityBuilder, Type attributeType )
        => eligibilityBuilder.MustSatisfy(
            d => d.Attributes.Any( attributeType ),
            d => $"{d} must have an attribute of type {attributeType.Name}" );

    /// <summary>
    /// Forbids the target declaration from having an attribute of a given type.
    /// </summary>
    /// <param name="eligibilityBuilder">The eligibility builder for a declaration.</param>
    /// <param name="attributeType">The attribute type to check for.</param>
    public static void MustNotHaveAttributeOfType( this IEligibilityBuilder<IDeclaration> eligibilityBuilder, Type attributeType )
        => eligibilityBuilder.MustSatisfy(
            d => !d.Attributes.Any( attributeType ),
            d => $"{d} must not have an attribute of type {attributeType.Name}" );

    internal static void MustNotBePartialMemberWithSourceGeneratorAttribute( this IEligibilityBuilder<IMember> eligibilityBuilder )
        => eligibilityBuilder.MustSatisfy(
            m => !m.Compilation.Project.ServiceProvider.GetRequiredService<ISourceGeneratorDetectionService>().IsWellKnownGeneratedDeclaration( m ),
            m => $"{m} must not be a partial member marked with source generator attribute" );

    /// <summary>
    /// Determines whether the given declaration is an eligible target for a specified aspect type given as a type parameter.
    /// </summary>
    /// <param name="declaration">The declaration for which eligibility is determined.</param>
    /// <param name="scenarios">The scenarios for which eligibility is determined. The default value is <see cref="EligibleScenarios.Default"/>.</param>
    /// <typeparam name="T">The aspect type.</typeparam>
    /// <returns><c>true</c> if <paramref name="declaration"/> is eligible for the aspect type <typeparamref name="T"/> for any of the specified <paramref name="scenarios"/>.</returns>
    public static bool IsAspectEligible<T>( this IDeclaration declaration, EligibleScenarios scenarios = EligibleScenarios.Default )
        where T : IAspect
        => MetalamaExecutionContext.Current.ServiceProvider.GetRequiredService<IEligibilityService>().IsEligible( typeof(T), declaration, scenarios );

    /// <summary>
    /// Determines whether the given declaration is an eligible target for a specified aspect type given as a reflection <see cref="Type"/>.
    /// </summary>
    /// <param name="declaration">The declaration for which eligibility is determined.</param>
    /// <param name="aspectType">The aspect type.</param>
    /// <param name="scenarios">The scenarios for which eligibility is determined. The default value is <see cref="EligibleScenarios.Default"/>.</param>
    /// <returns><c>true</c> if <paramref name="declaration"/> is eligible for the given <paramref name="aspectType"/> for any of the specified <paramref name="scenarios"/>.</returns>
    public static bool IsAspectEligible( this IDeclaration declaration, Type aspectType, EligibleScenarios scenarios = EligibleScenarios.Default )
        => MetalamaExecutionContext.Current.ServiceProvider.GetRequiredService<IEligibilityService>().IsEligible( aspectType, declaration, scenarios );

    /// <summary>
    /// Determines whether the given declaration is an eligible target for a specified kind of advice.
    /// </summary>
    /// <param name="declaration">The declaration for which eligibility is determined.</param>
    /// <param name="adviceKind">Tha advice kind, but not <see cref="AdviceKind.AddContract"/>.</param>
    /// <returns><c>true</c> if <paramref name="declaration"/> is eligible for the given <paramref name="adviceKind"/>.</returns>
    /// <seealso cref="IsContractAdviceEligible"/>
    public static bool IsAdviceEligible( this IDeclaration declaration, AdviceKind adviceKind )
        => (EligibilityRuleFactory.GetAdviceEligibilityRule( adviceKind ).GetEligibility( declaration ) & EligibleScenarios.Default) != 0;

    /// <summary>
    ///  Determines whether the given declaration is an eligible target for an <see cref="AdviceKind.AddContract"/> advice for a given <see cref="ContractDirection"/>.
    /// </summary>
    /// <param name="declaration">The declaration for which eligibility is determined.</param>
    /// <param name="contractDirection">The contract direction.</param>
    /// <returns><c>true</c> if <paramref name="declaration"/> is eligible for an <see cref="AdviceKind.AddContract"/> advice for the given <paramref name="contractDirection"/>.</returns>
    public static bool IsContractAdviceEligible( this IDeclaration declaration, ContractDirection contractDirection = ContractDirection.Default )
        => (EligibilityRuleFactory.GetContractAdviceEligibilityRule( contractDirection ).GetEligibility( declaration ) & EligibleScenarios.Default) != 0;
}