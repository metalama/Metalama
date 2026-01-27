// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Eligibility;

public static partial class EligibilityRuleFactory
{
    private static class Contracts
    {
        private static readonly IEligibilityRule<IDeclaration> _contractEligibilityBoth;
        private static readonly IEligibilityRule<IDeclaration> _contractEligibilityInput;
        private static readonly IEligibilityRule<IDeclaration> _contractEligibilityOutput;
        private static readonly IEligibilityRule<IDeclaration> _contractEligibilityDefault;

        static Contracts()
        {
            var declaringTypeRule = CreateRule<INamedType>( builder => builder.MustBeRunTimeOnly() );

            // Eligibility rules for fields, properties and indexers. Note that we always skip constant fields.
            static void AddCommonGetterParameterRules( IEligibilityBuilder<IFieldOrPropertyOrIndexer> builder )
            {
                builder.MustSatisfy(
                    p => p.GetMethod?.GetIteratorInfo().EnumerableKind is not (EnumerableKind.IAsyncEnumerable or EnumerableKind.IAsyncEnumerator),
                    member
                        => $"{member} must not have get accessor that returns IAsyncEnumerable<T> or IAsyncEnumerator<T>" );
            }

            var propertyOrIndexerEligibilityInput =
                CreateRule<IFieldOrPropertyOrIndexer>(
                    builder =>
                    {
                        builder.MustBeWritable();
                        builder.MustBeExplicitlyDeclared();
                        builder.DeclaringType().AddRule( declaringTypeRule );
                        builder.ExceptForInheritance().MustNotBeAbstract();
                    } );

            var propertyOrIndexerEligibilityOutput =
                CreateRule<IFieldOrPropertyOrIndexer>(
                    fieldOrPropertyOrIndexer
                        => fieldOrPropertyOrIndexer.Convert()
                            .When<IPropertyOrIndexer>()
                            .AddRules(
                                builder =>
                                {
                                    builder.MustBeReadable();
                                    builder.MustBeExplicitlyDeclared();
                                    AddCommonGetterParameterRules( builder );
                                    builder.DeclaringType().AddRule( declaringTypeRule );
                                    builder.ExceptForInheritance().MustNotBeAbstract();
                                } ) );

            var propertyOrIndexerEligibilityBoth =
                CreateRule<IFieldOrPropertyOrIndexer>(
                    builder =>
                    {
                        builder.MustBeReadable();
                        builder.MustBeWritable();
                        AddCommonGetterParameterRules( builder );
                        builder.DeclaringType().AddRule( declaringTypeRule );
                        builder.ExceptForInheritance().MustNotBeAbstract();
                    } );

            var propertyOrIndexerEligibilityDefault =
                CreateRule<IFieldOrPropertyOrIndexer>(
                    builder =>
                    {
                        builder.MustBeExplicitlyDeclared();
                        AddCommonGetterParameterRules( builder );
                        builder.Convert().When<IField>().MustBeWritable();
                        builder.DeclaringType().AddRule( declaringTypeRule );
                        builder.ExceptForInheritance().MustNotBeAbstract();
                    } );

            // Eligibility rules for regular parameters (not extension block receiver parameters).
            static void AddCommonParameterRules( IEligibilityBuilder<IParameter> parameter )
            {
                parameter.DeclaringMember().MustBeExplicitlyDeclared();
                parameter.ExceptForInheritance().DeclaringMember().MustNotBeAbstract();

                parameter.DeclaringMember()
                    .Convert()
                    .When<IMethod>()
                    .AddRules(
                        method =>
                            method.MustSatisfy(
                                m => !(m is { IsPartial: true, HasImplementation: false }),
                                m => $"'{m}' must not be partial without an implementation" ) );
            }

            // Eligibility rules for parameters.
            static void AddCommonReturnParameterRules( IEligibilityBuilder<IParameter> parameter )
            {
                parameter.MustNotBeVoid();

                parameter.MustSatisfy(
                    p => !(p is { IsReturnParameter: true, DeclaringMember: IMethod method } && method.GetAsyncInfo().ResultType.Equals( SpecialType.Void )),
                    member => $"{member} must not have void awaitable result" );
            }

            // Helper to check if parameter is a receiver parameter of an extension block.
            static bool IsReceiverParameter( IParameter p ) => p.ContainingDeclaration is IExtensionBlock;

            // Eligibility rules for receiver parameters of extension blocks.
            // Receiver parameters have a different structure - they don't have a DeclaringMember but a containing extension block.
            var receiverParameterEligibilityInput =
                CreateRule<IParameter>(
                    parameter =>
                    {
                        parameter.MustSatisfy( IsReceiverParameter, p => $"{p} must be a receiver parameter of an extension block" );
                        parameter.MustBeReadable();
                    } );

            var receiverParameterEligibilityOutput =
                CreateRule<IParameter>(
                    parameter =>
                    {
                        parameter.MustSatisfy( IsReceiverParameter, p => $"{p} must be a receiver parameter of an extension block" );
                        parameter.MustBeWritable();
                    } );

            var receiverParameterEligibilityBoth =
                CreateRule<IParameter>(
                    parameter =>
                    {
                        parameter.MustSatisfy( IsReceiverParameter, p => $"{p} must be a receiver parameter of an extension block" );
                        parameter.MustBeRef();
                    } );

            var receiverParameterEligibilityDefault =
                CreateRule<IParameter>(
                    parameter =>
                    {
                        parameter.MustSatisfy( IsReceiverParameter, p => $"{p} must be a receiver parameter of an extension block" );
                    } );

            // Eligibility rules for regular parameters.
            var regularParameterEligibilityInput =
                CreateRule(
                    (Action<IEligibilityBuilder<IParameter>>) (parameter =>
                    {
                        parameter.MustNotBeReturnParameter();
                        parameter.MustBeReadable();
                        AddCommonParameterRules( parameter );
                        AddCommonReturnParameterRules( parameter );
                        parameter.DeclaringMember().DeclaringType().AddRule( declaringTypeRule );
                    }) );

            var regularParameterEligibilityOutput =
                CreateRule<IParameter>(
                    parameter =>
                    {
                        parameter.MustSatisfyAny( p => p.MustBeWritable(), p => p.MustBeReturnParameter() );
                        parameter.MustSatisfy( p => p.DeclaringMember is not IConstructor, _ => $"output contracts on constructors are not supported" );
                        AddCommonParameterRules( parameter );
                        AddCommonReturnParameterRules( parameter );
                        parameter.DeclaringMember().DeclaringType().AddRule( declaringTypeRule );
                    } );

            var regularParameterEligibilityBoth =
                CreateRule<IParameter>(
                    parameter =>
                    {
                        parameter.MustNotBeReturnParameter();
                        parameter.MustBeRef();
                        parameter.MustSatisfy( p => p.DeclaringMember is not IConstructor, _ => $"output contracts on constructors are not supported" );
                        AddCommonParameterRules( parameter );
                        AddCommonReturnParameterRules( parameter );
                        parameter.DeclaringMember().DeclaringType().AddRule( declaringTypeRule );
                    } );

            var regularParameterEligibilityDefault =
                CreateRule<IParameter>(
                    parameter =>
                    {
                        parameter.MustSatisfy(
                            p => !(p is { RefKind: RefKind.Out, DeclaringMember: IConstructor }),
                            _ => $"output contracts on constructors are not supported" );

                        AddCommonParameterRules( parameter );
                        AddCommonReturnParameterRules( parameter );
                        parameter.DeclaringMember().DeclaringType().AddRule( declaringTypeRule );
                    } );

            // Combined parameter eligibility rules that handle both regular and receiver parameters.
            // The receiver parameter eligibility rules already check that the parameter IS a receiver parameter.
            // The regular parameter eligibility rules use DeclaringMember() which fails on receiver parameters.
            // So receiver parameters will satisfy receiverParameterEligibility, and regular parameters will satisfy regularParameterEligibility.
            var parameterEligibilityInput =
                CreateRule<IParameter>(
                    parameter => parameter.MustSatisfyAny(
                        p => p.AddRule( receiverParameterEligibilityInput ),
                        p => p.AddRule( regularParameterEligibilityInput ) ) );

            var parameterEligibilityOutput =
                CreateRule<IParameter>(
                    parameter => parameter.MustSatisfyAny(
                        p => p.AddRule( receiverParameterEligibilityOutput ),
                        p => p.AddRule( regularParameterEligibilityOutput ) ) );

            var parameterEligibilityBoth =
                CreateRule<IParameter>(
                    parameter => parameter.MustSatisfyAny(
                        p => p.AddRule( receiverParameterEligibilityBoth ),
                        p => p.AddRule( regularParameterEligibilityBoth ) ) );

            var parameterEligibilityDefault =
                CreateRule<IParameter>(
                    parameter => parameter.MustSatisfyAny(
                        p => p.AddRule( receiverParameterEligibilityDefault ),
                        p => p.AddRule( regularParameterEligibilityDefault ) ) );

            _contractEligibilityBoth = CreateRule<IDeclaration>(
                d =>
                {
                    d.MustBeInstanceOfAnyType( typeof(IParameter), typeof(IFieldOrPropertyOrIndexer) );
                    d.Convert().When<IParameter>().AddRule( parameterEligibilityBoth );
                    d.Convert().When<IFieldOrPropertyOrIndexer>().AddRule( propertyOrIndexerEligibilityBoth );
                } );

            _contractEligibilityInput = CreateRule<IDeclaration>(
                d =>
                {
                    d.MustBeInstanceOfAnyType( typeof(IParameter), typeof(IFieldOrPropertyOrIndexer) );
                    d.Convert().When<IParameter>().AddRule( parameterEligibilityInput );
                    d.Convert().When<IFieldOrPropertyOrIndexer>().AddRule( propertyOrIndexerEligibilityInput );
                } );

            _contractEligibilityOutput = CreateRule<IDeclaration>(
                d =>
                {
                    d.MustBeInstanceOfAnyType( typeof(IParameter), typeof(IFieldOrPropertyOrIndexer) );
                    d.Convert().When<IParameter>().AddRule( parameterEligibilityOutput );
                    d.Convert().When<IFieldOrPropertyOrIndexer>().AddRule( propertyOrIndexerEligibilityOutput );
                } );

            _contractEligibilityDefault = CreateRule<IDeclaration>(
                d =>
                {
                    d.MustBeInstanceOfAnyType( typeof(IParameter), typeof(IFieldOrPropertyOrIndexer) );

                    d.Convert()
                        .When<IParameter>()
                        .AddRule( parameterEligibilityDefault );

                    d.Convert().When<IFieldOrPropertyOrIndexer>().AddRule( propertyOrIndexerEligibilityDefault );
                } );
        }

        public static IEligibilityRule<IDeclaration> GetEligibilityRule( ContractDirection direction )
            => direction switch
            {
                ContractDirection.Default => _contractEligibilityDefault,
                ContractDirection.Both => _contractEligibilityBoth,
                ContractDirection.Input => _contractEligibilityInput,
                ContractDirection.Output => _contractEligibilityOutput,
                _ => throw new ArgumentOutOfRangeException( nameof(direction) )
            };
    }
}