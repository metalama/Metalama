// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility.Implementation;
using System;
using System.Linq;

namespace Metalama.Framework.Eligibility;

/// <summary>
/// Factory methods for creating instances of the <see cref="IEligibilityRule{T}"/> interface, including predefined rules
/// for standard advice kinds and methods for creating custom rules.
/// </summary>
/// <remarks>
/// <para>
/// This class provides:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="GetAdviceEligibilityRule"/> - Gets the default eligibility rules for built-in advice kinds (Override, Introduce, etc.).</description></item>
/// <item><description><see cref="GetContractAdviceEligibilityRule"/> - Gets the default eligibility rules for contract advice.</description></item>
/// <item><description><see cref="CreateRule{T}"/> - Creates custom eligibility rules using a builder pattern.</description></item>
/// </list>
/// <para>
/// Use these methods when implementing <see cref="IEligible{T}.BuildEligibility"/> manually or when you need to verify
/// eligibility programmatically using <see cref="IAspectBuilder{TAspectTarget}.VerifyEligibility"/>.
/// </para>
/// </remarks>
/// <seealso cref="IEligibilityRule{T}"/>
/// <seealso href="@eligibility"/>
[CompileTime]
[PublicAPI]
public static partial class EligibilityRuleFactory
{
    private static readonly IEligibilityRule<IDeclaration> _overrideDeclaringTypeRule = CreateRule<IDeclaration, INamedType>(
        builder =>
        {
            builder.MustBeRunTimeOnly();
            builder.MustNotBeRef();

            builder.ExceptForInheritance()
                .MustSatisfy(
                    t => t.TypeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Interface or TypeKind.Extension,
                    t => $"'{t}' is neither a class, struct, interface, nor extensions block" );
        } );

    internal static IEligibilityRule<IDeclaration> OverrideConstructorAdviceRule { get; } = CreateRule<IDeclaration, IConstructor>(
        builder =>
        {
            builder.MustNotBeRecordCopyConstructor();
            builder.DeclaringType().AddRule( _overrideDeclaringTypeRule );
        } );

    internal static IEligibilityRule<IDeclaration> OverrideMethodAdviceRule { get; } = CreateRule<IDeclaration, IMethod>(
        builder =>
        {
            builder.ExceptForInheritance().MustNotBeAbstract();

            builder.MustSatisfy(
                m => m is
                     {
                         ContainingDeclaration.IsImplicitlyDeclared: false,
                         MethodKind: MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise or MethodKind.PropertyGet or MethodKind.PropertySet
                     }
                     || !m.IsImplicitlyDeclared,
                m => $"{m} must be an accessor or an explicitly declared method" );

            builder.MustNotBeRef();
            builder.MustSatisfy( m => !m.IsExtern, m => $"'{m}' must not be extern" );

            builder.MustSatisfy(
                m => !(m is { MethodKind: MethodKind.PropertyGet, DeclaringMember: IField { Writeability: Writeability.None } }),
                m => $"'{m}' must not be the getter of a const field." );

            builder.MustNotBePartialMemberWithSourceGeneratorAttribute();
            builder.DeclaringType().AddRule( _overrideDeclaringTypeRule );
        } );

    internal static IEligibilityRule<IDeclaration> OverrideFieldOrPropertyOrIndexerAdviceRule { get; } =
        CreateRule<IDeclaration, IFieldOrPropertyOrIndexer>(
            builder =>
            {
                builder.ExceptForInheritance().MustNotBeAbstract();
                builder.MustBeExplicitlyDeclared();
                builder.MustSatisfy( d => d is not IField { Writeability: Writeability.None }, d => $"{d} must not be a constant" );
                builder.MustNotBeRef();
                builder.MustNotBePartialMemberWithSourceGeneratorAttribute();
                builder.DeclaringType().AddRule( _overrideDeclaringTypeRule );
            } );

    internal static IEligibilityRule<IDeclaration> OverrideEventAdviceRule { get; } = CreateRule<IDeclaration, IEvent>(
        builder =>
        {
            builder.ExceptForInheritance().MustNotBeAbstract();
            builder.MustBeExplicitlyDeclared();
            builder.DeclaringType().AddRule( _overrideDeclaringTypeRule );
        } );

    internal static IEligibilityRule<IDeclaration> OverrideEventRaiseAdviceRule { get; } = CreateRule<IDeclaration, IEvent>(
        builder =>
        {
            builder.MustSatisfy(
                e => e.Type.Methods.OfName( "Invoke" ).Single().ReturnType.SpecialType == SpecialType.Void,
                e => $"'{e}' must have delegate type with void return value" );

            builder.MustSatisfy(
                e => e.Type.Methods.OfName( "Invoke" ).Single().Parameters.All( p => p.RefKind == RefKind.None ),
                e => $"'{e}' must have delegate type without a parameter of out/ref/in/pointer type" );

            builder.DeclaringType().AddRule( _overrideDeclaringTypeRule );
        } );

    private static readonly IEligibilityRule<IDeclaration> _introduceRule = CreateRule<IDeclaration, INamedType>(
        builder =>
        {
            builder.MustSatisfy(
                t => t.TypeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Interface or TypeKind.Extension,
                t => $"'{t}' must be a class, struct, interface, or extension block" );

            builder.MustBeExplicitlyDeclared();
            builder.MustBeRunTimeOnly();
        } );

    private static readonly IEligibilityRule<IDeclaration> _introduceExtensionBlockRule = CreateRule<IDeclaration, INamedType>(
        builder =>
        {
            builder.MustSatisfy(
                t => t is { TypeKind: TypeKind.Class, IsStatic: true, DeclaringType: null },
                t => $"'{t}' must be a top-level static class" );

            builder.MustBeExplicitlyDeclared();
            builder.MustBeRunTimeOnly();
        } );

    private static readonly IEligibilityRule<IDeclaration> _implementInterfaceRule = CreateRule<IDeclaration, INamedType>(
        builder =>
        {
            builder.MustSatisfy(
                t => t.TypeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Interface,
                t => $"'{t}' must be a class, struct, or interface" );

            builder.MustBeExplicitlyDeclared();
            builder.MustNotBeStatic();
            builder.MustBeRunTimeOnly();
            builder.MustNotBeExtensionBlock();
        } );

    private static readonly IEligibilityRule<IDeclaration> _introduceParameterRule = CreateRule<IDeclaration, IConstructor>(
        builder =>
        {
            builder.DeclaringType().MustBeRunTimeOnly();
            builder.MustNotBeStatic();
            builder.MustNotBeRecordCopyConstructor();

            builder.MustSatisfy(
                c => c.Parameters.All( p => !p.IsParams ),
                c => $"'{c}' must not have params parameter" );
        } );

    private static readonly IEligibilityRule<IDeclaration> _addInitializerRule = CreateRule<IDeclaration, IMemberOrNamedType>(
        builder =>
        {
            builder.MustBeInstanceOfAnyType( typeof(INamedType), typeof(IConstructor) );

            builder.Convert()
                .When<INamedType>()
                .AddRules(
                    typeEligibilityBuilder =>
                    {
                        typeEligibilityBuilder.MustSatisfy(
                            t => t.TypeKind is TypeKind.Class or TypeKind.Struct,
                            t => $"'{t}' must be a class or struct" );

                        typeEligibilityBuilder.MustBeExplicitlyDeclared();
                        typeEligibilityBuilder.MustBeRunTimeOnly();
                        typeEligibilityBuilder.MustNotBeExtensionBlock();
                    } );

            builder.Convert()
                .When<IConstructor>()
                .AddRules(
                    constructorEligibilityBuilder =>
                    {
                        constructorEligibilityBuilder.MustNotBeRecordCopyConstructor();
                        constructorEligibilityBuilder.MustNotBeStatic();
                        constructorEligibilityBuilder.MustNotBeExtensionMember();

                        constructorEligibilityBuilder.DeclaringType().MustBeExplicitlyDeclared();
                        constructorEligibilityBuilder.DeclaringType().MustBeRunTimeOnly();
                    } );
        } );

    private static readonly IEligibilityRule<IDeclaration> _introduceAttributeRule = CreateRule<IDeclaration, IDeclaration>(
        builder => builder.MustSatisfyAny(

            // Allow explicitly-declared declarations
            b => b.MustBeExplicitlyDeclared(),

            // Allow return value parameters of explicitly-declared members
            b => b.Convert()
                .To<IParameter>()
                .MustSatisfy(
                    p => p is { IsReturnParameter: true } or
                        { DeclaringMember: { IsImplicitlyDeclared: false } or IMethod { MethodKind: MethodKind.PropertySet } },
                    p => $"{p} must be a return parameter of an explicitly-declared member or the `value` parameter of a property setter" ),

            // Allow default constructors (implicitly declared)
            b => b.Convert()
                .To<IConstructor>()
                .MustSatisfy(
                    c => c is { IsImplicitlyDeclared: true, Parameters.Count: 0 },
                    c => $"{c} must be an implicit default constructor" ),

            // Allow backing fields of auto-properties (implicitly declared)
            b => b.Convert()
                .To<IField>()
                .MustSatisfy(
                    f => f.IsAutoPropertyBackingField(),
                    f => $"{f} must be an auto-property backing field" ) ) );

    /// <summary>
    /// Gets the default eligibility rules that apply to a specific advice kind.
    /// </summary>
    /// <param name="adviceKind">The kind of advice (e.g., <see cref="AdviceKind.OverrideMethod"/>, <see cref="AdviceKind.IntroduceField"/>).</param>
    /// <returns>An <see cref="IEligibilityRule{T}"/> that encapsulates the default eligibility requirements for the specified advice kind.</returns>
    /// <remarks>
    /// <para>
    /// The rules returned by this method are those used by built-in aspect classes such as <see cref="OverrideMethodAspect"/>,
    /// <see cref="OverrideFieldOrPropertyAspect"/>, and others. If you implement the <see cref="IEligible{T}.BuildEligibility"/>
    /// method manually, you can use this method to get the base rules and add only rules that are specific to your aspect.
    /// </para>
    /// <para>
    /// For contract advice (<see cref="AdviceKind.AddContract"/>), use <see cref="GetContractAdviceEligibilityRule"/> instead.
    /// </para>
    /// </remarks>
    /// <seealso cref="GetContractAdviceEligibilityRule"/>
    public static IEligibilityRule<IDeclaration> GetAdviceEligibilityRule( AdviceKind adviceKind )
        => adviceKind switch
        {
            AdviceKind.None => EligibilityRule<IDeclaration>.Empty,
            AdviceKind.OverrideConstructor => OverrideConstructorAdviceRule,
            AdviceKind.OverrideMethod => OverrideMethodAdviceRule,
            AdviceKind.OverrideFieldOrPropertyOrIndexer => OverrideFieldOrPropertyOrIndexerAdviceRule,
            AdviceKind.OverrideEvent => OverrideEventAdviceRule,
            AdviceKind.OverrideEventInvoke => OverrideEventRaiseAdviceRule,
            AdviceKind.IntroduceMethod => _introduceRule,
            AdviceKind.IntroduceFinalizer => _introduceRule,
#pragma warning disable CS0618 // IntroduceOperator is obsolete but needs to be handled for backward compatibility
            AdviceKind.IntroduceOperator => _introduceRule,
#pragma warning restore CS0618
            AdviceKind.IntroduceField => _introduceRule,
            AdviceKind.IntroduceEvent => _introduceRule,
            AdviceKind.IntroduceProperty => _introduceRule,
            AdviceKind.IntroduceIndexer => _introduceRule,
            AdviceKind.IntroduceConstructor => _introduceRule,
            AdviceKind.IntroduceExtensionBlock => _introduceExtensionBlockRule,
            AdviceKind.ImplementInterface => _implementInterfaceRule,
            AdviceKind.AddInitializer => _addInitializerRule,
            AdviceKind.IntroduceParameter => _introduceParameterRule,
            AdviceKind.IntroduceAttribute => _introduceAttributeRule,
            _ => throw new ArgumentOutOfRangeException( nameof(adviceKind), $"Value not supported: {adviceKind}." )
        };

    /// <summary>
    /// Gets the default eligibility rules that apply to a contract advice for a specific contract direction.
    /// </summary>
    /// <param name="contractDirection">The contract direction (e.g., <see cref="ContractDirection.Input"/>, <see cref="ContractDirection.Output"/>).</param>
    /// <returns>An <see cref="IEligibilityRule{T}"/> that encapsulates the default eligibility requirements for contract advice with the specified direction.</returns>
    /// <remarks>
    /// <para>
    /// The rules returned by this method are those used by the <see cref="ContractAspect"/> class.
    /// If you implement the <see cref="IEligible{T}.BuildEligibility"/> method manually, you can use this method to get the base rules
    /// and add only rules that are specific to your aspect.
    /// </para>
    /// </remarks>
    /// <seealso cref="GetAdviceEligibilityRule"/>
    /// <seealso cref="ContractAspect"/>
    public static IEligibilityRule<IDeclaration> GetContractAdviceEligibilityRule( ContractDirection contractDirection )
        => Contracts.GetEligibilityRule( contractDirection );

    /// <summary>
    /// Creates a custom instance of the <see cref="IEligibilityRule{T}"/> interface using a builder pattern.
    /// </summary>
    /// <typeparam name="T">The type of declaration that the rule validates.</typeparam>
    /// <param name="predicate">An action that configures the eligibility builder by adding rules using methods from <see cref="EligibilityExtensions"/>.</param>
    /// <param name="otherPredicates">Additional actions that configure the eligibility builder. All actions are combined with AND logic.</param>
    /// <returns>An immutable <see cref="IEligibilityRule{T}"/> that can be used to validate declarations.</returns>
    /// <remarks>
    /// <para>
    /// Eligibility rules are relatively expensive objects to create, although their evaluation is fast and efficient.
    /// It is strongly recommended to store rules in static fields of the aspect class to avoid recreating them for each aspect instance.
    /// </para>
    /// <para>
    /// The created rule can be used with <see cref="IAspectBuilder{TAspectTarget}.VerifyEligibility"/> to programmatically validate
    /// eligibility within aspect logic.
    /// </para>
    /// </remarks>
    public static IEligibilityRule<T> CreateRule<T>( Action<IEligibilityBuilder<T>> predicate, params Action<IEligibilityBuilder<T>>[]? otherPredicates )
        where T : class
    {
        var builder = new EligibilityBuilder<T>();
        predicate( builder );

        if ( otherPredicates != null )
        {
            foreach ( var otherPredicate in otherPredicates )
            {
                otherPredicate( builder );
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// Creates a custom instance of the <see cref="IEligibilityRule{T}"/> interface that validates a general type but requires
    /// a more specific type for the rule definition.
    /// </summary>
    /// <typeparam name="TGeneral">The general type of declaration that the rule can validate (e.g., <see cref="IDeclaration"/>).</typeparam>
    /// <typeparam name="TRequired">The more specific type required for building the rule (e.g., <see cref="IMethod"/>). Must be derived from <typeparamref name="TGeneral"/>.</typeparam>
    /// <param name="predicate">An action that configures the eligibility builder for <typeparamref name="TRequired"/> by adding rules.</param>
    /// <param name="otherPredicates">Additional actions that configure the eligibility builder. All actions are combined with AND logic.</param>
    /// <returns>An immutable <see cref="IEligibilityRule{T}"/> for <typeparamref name="TGeneral"/> that implicitly requires the declaration to be of type <typeparamref name="TRequired"/>.</returns>
    /// <remarks>
    /// <para>
    /// This overload automatically adds a type conversion requirement (using <see cref="EligibilityExtensions.Converter{T}.To{TOutput}"/>), making declarations
    /// ineligible if they are not of type <typeparamref name="TRequired"/>.
    /// </para>
    /// <para>
    /// Eligibility rules are relatively expensive objects to create. Store them in static fields to avoid recreating them for each aspect instance.
    /// </para>
    /// </remarks>
    public static IEligibilityRule<TGeneral> CreateRule<TGeneral, TRequired>(
        Action<IEligibilityBuilder<TRequired>> predicate,
        params Action<IEligibilityBuilder<TRequired>>[]? otherPredicates )
        where TGeneral : class
        where TRequired : class, TGeneral
    {
        var generalBuilder = new EligibilityBuilder<TGeneral>();

        var requiredBuilder = generalBuilder.Convert().To<TRequired>();
        predicate( requiredBuilder );

        if ( otherPredicates != null )
        {
            foreach ( var otherPredicate in otherPredicates )
            {
                otherPredicate( requiredBuilder );
            }
        }

        return generalBuilder.Build();
    }
}