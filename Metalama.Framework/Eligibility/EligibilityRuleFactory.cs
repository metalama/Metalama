// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility.Implementation;
using System;
using System.Linq;

namespace Metalama.Framework.Eligibility;

/// <summary>
/// Creates instances of the <see cref="IEligibilityRule{T}"/> interface, which can then be used by the <see cref="IAspectBuilder{TAspectTarget}.VerifyEligibility"/> method.
/// </summary>
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
                    t => t.TypeKind is TypeKind.Class or TypeKind.RecordClass or TypeKind.Struct or TypeKind.RecordStruct or TypeKind.Interface,
                    t => $"'{t}' is neither a class, record class, struct, record struct, nor interface" );
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
            builder.MustBeExplicitlyDeclared();
            builder.MustNotBeRef();
            builder.MustSatisfy( m => !m.IsExtern, m => $"'{m}' must not be extern" );
            builder.DeclaringType().AddRule( _overrideDeclaringTypeRule );
        } );

    internal static IEligibilityRule<IDeclaration> OverrideFieldOrPropertyOrIndexerAdviceRule { get; } = CreateRule<IDeclaration, IFieldOrPropertyOrIndexer>(
        builder =>
        {
            builder.ExceptForInheritance().MustNotBeAbstract();
            builder.MustBeExplicitlyDeclared();
            builder.MustSatisfy( d => d is not IField { Writeability: Writeability.None }, d => $"{d} must not be a constant" );
            builder.MustNotBeRef();
            builder.DeclaringType().AddRule( _overrideDeclaringTypeRule );
        } );

    internal static IEligibilityRule<IDeclaration> OverrideEventAdviceRule { get; } = CreateRule<IDeclaration, IEvent>(
        builder =>
        {
            builder.ExceptForInheritance().MustNotBeAbstract();
            builder.MustBeExplicitlyDeclared();
            builder.DeclaringType().AddRule( _overrideDeclaringTypeRule );
        } );

    private static readonly IEligibilityRule<IDeclaration> _introduceRule = CreateRule<IDeclaration, INamedType>(
        builder =>
        {
            builder.MustSatisfy(
                t => t.TypeKind is TypeKind.Class or TypeKind.RecordClass or TypeKind.Struct or TypeKind.RecordStruct or TypeKind.Interface,
                t => $"'{t}' must be a class, record class, struct, record struct, or interface" );

            builder.MustBeExplicitlyDeclared();
            builder.MustBeRunTimeOnly();
        } );

    private static readonly IEligibilityRule<IDeclaration> _implementInterfaceRule = CreateRule<IDeclaration, INamedType>(
        builder =>
        {
            builder.MustSatisfy(
                t => t.TypeKind is TypeKind.Class or TypeKind.RecordClass or TypeKind.Struct or TypeKind.RecordStruct or TypeKind.Interface,
                t => $"'{t}' must be a class, record class, struct, or record struct" );

            builder.MustBeExplicitlyDeclared();
            builder.MustNotBeStatic();
            builder.MustBeRunTimeOnly();
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
                            t => t.TypeKind is TypeKind.Class or TypeKind.RecordClass or TypeKind.Struct or TypeKind.RecordStruct,
                            t => $"'{t}' must be a class, record class, struct, or record struct" );

                        typeEligibilityBuilder.MustBeExplicitlyDeclared();
                        typeEligibilityBuilder.MustBeRunTimeOnly();
                    } );

            builder.Convert()
                .When<IConstructor>()
                .AddRules(
                    constructorEligibilityBuilder =>
                    {
                        constructorEligibilityBuilder.MustNotBeRecordCopyConstructor();
                        constructorEligibilityBuilder.MustNotBeStatic();

                        constructorEligibilityBuilder.DeclaringType().MustBeExplicitlyDeclared();
                        constructorEligibilityBuilder.DeclaringType().MustBeRunTimeOnly();
                    } );
        } );

    /// <summary>
    /// Gets the default eligibility rules that apply to a specific advice.
    /// The rules returned by this method are those used by classes <see cref="OverrideMethodAspect"/>, <see cref="OverrideFieldOrPropertyAspect"/>
    /// and so on. If you implement the <see cref="IEligible{T}.BuildEligibility"/> method manually, you can use this method to get the base rules, and
    /// add only rules that are specific to your aspect.
    /// </summary>
    /// <param name="adviceKind">The kind of advice.</param>
    public static IEligibilityRule<IDeclaration> GetAdviceEligibilityRule( AdviceKind adviceKind )
        => adviceKind switch
        {
            AdviceKind.None => EligibilityRule<IDeclaration>.Empty,
            AdviceKind.OverrideConstructor => OverrideConstructorAdviceRule,
            AdviceKind.OverrideMethod => OverrideMethodAdviceRule,
            AdviceKind.OverrideFieldOrPropertyOrIndexer => OverrideFieldOrPropertyOrIndexerAdviceRule,
            AdviceKind.OverrideEvent => OverrideEventAdviceRule,
            AdviceKind.IntroduceMethod => _introduceRule,
            AdviceKind.IntroduceFinalizer => _introduceRule,
            AdviceKind.IntroduceOperator => _introduceRule,
            AdviceKind.IntroduceField => _introduceRule,
            AdviceKind.IntroduceEvent => _introduceRule,
            AdviceKind.IntroduceProperty => _introduceRule,
            AdviceKind.IntroduceIndexer => _introduceRule,
            AdviceKind.IntroduceConstructor => _introduceRule,
            AdviceKind.ImplementInterface => _implementInterfaceRule,
            AdviceKind.AddInitializer => _addInitializerRule,
            AdviceKind.IntroduceParameter => _introduceParameterRule,
            _ => throw new ArgumentOutOfRangeException( nameof(adviceKind), $"Value not supported: {adviceKind}." )
        };

    /// <summary>
    /// Gets the default eligibility rules that apply to a contract advice for a specific direction.
    /// The rules returned by this method are those used by the <see cref="ContractAspect"/> class.
    /// If you implement the <see cref="IEligible{T}.BuildEligibility"/> method manually, you can use this method to get the base rules, and
    /// add only rules that are specific to your aspect.
    /// </summary>
    public static IEligibilityRule<IDeclaration> GetContractAdviceEligibilityRule( ContractDirection contractDirection )
        => Contracts.GetEligibilityRule( contractDirection );

    /// <summary>
    /// Create an instance of the <see cref="IEligibilityRule{T}"/> interface, which can then be used by the <see cref="IAspectBuilder{TAspectTarget}.VerifyEligibility"/> method.
    /// </summary>
    /// <remarks>
    /// Eligibility rules are heavy and expensive objects although their evaluation is fast and efficient. It is recommended to store rules in static fields of the aspect. 
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