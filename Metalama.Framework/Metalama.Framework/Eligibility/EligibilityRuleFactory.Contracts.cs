// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

            // Eligibility rules for parameters.
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

            var parameterEligibilityInput =
                CreateRule(
                    (Action<IEligibilityBuilder<IParameter>>) (parameter =>
                    {
                        parameter.MustNotBeReturnParameter();
                        parameter.MustBeReadable();
                        AddCommonParameterRules( parameter );
                        AddCommonReturnParameterRules( parameter );
                        parameter.DeclaringMember().DeclaringType().AddRule( declaringTypeRule );
                    }) );

            var parameterEligibilityOutput =
                CreateRule<IParameter>(
                    parameter =>
                    {
                        parameter.MustSatisfyAny( p => p.MustBeWritable(), p => p.MustBeReturnParameter() );
                        parameter.MustSatisfy( p => p.DeclaringMember is not IConstructor, _ => $"output contracts on constructors are not supported" );
                        AddCommonParameterRules( parameter );
                        AddCommonReturnParameterRules( parameter );
                        parameter.DeclaringMember().DeclaringType().AddRule( declaringTypeRule );
                    } );

            var parameterEligibilityBoth =
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

            var parameterEligibilityDefault =
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
                    d.Convert().When<IParameter>().AddRule( parameterEligibilityDefault );
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