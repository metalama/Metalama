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
                parameter.MustSatisfy(
                    p => p.DeclaringMember is not IMethod { MethodKind: MethodKind.DelegateInvoke },
                    p => $"the declaring member of {p} must not be a delegate member" );

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
            // Note: The IsReceiverParameter check is not included here because these rules are applied conditionally
            // via If(IsReceiverParameter) in the combined parameter eligibility rules below.
            var receiverParameterEligibilityInput =
                CreateRule<IParameter>( parameter => parameter.MustBeReadable() );

            var receiverParameterEligibilityOutput =
                CreateRule<IParameter>( parameter => parameter.MustBeWritable() );

            var receiverParameterEligibilityBoth =
                CreateRule<IParameter>( parameter => parameter.MustBeRef() );

            // Receiver parameters with Default direction have no additional constraints beyond being a receiver parameter,
            // which is already checked by the If(IsReceiverParameter) wrapper.
            var receiverParameterEligibilityDefault = CreateRule<IParameter>( _ => { } );

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
                        AddCommonParameterRules( parameter );
                        AddCommonReturnParameterRules( parameter );
                        parameter.DeclaringMember().DeclaringType().AddRule( declaringTypeRule );
                    } );

            var regularParameterEligibilityDefault =
                CreateRule<IParameter>(
                    parameter =>
                    {
                        AddCommonParameterRules( parameter );
                        AddCommonReturnParameterRules( parameter );
                        parameter.DeclaringMember().DeclaringType().AddRule( declaringTypeRule );
                    } );

            // Combined parameter eligibility rules that handle both regular and receiver parameters.
            // We use conditional If() instead of MustSatisfyAny() to avoid confusing error messages
            // that mention irrelevant alternatives (e.g., regular parameter errors mentioning "must be a receiver parameter").
            var parameterEligibilityInput =
                CreateRule<IParameter>(
                    parameter =>
                    {
                        // Apply receiver-specific rules only to receiver parameters
                        parameter.If( IsReceiverParameter ).AddRule( receiverParameterEligibilityInput );

                        // Apply regular parameter rules only to non-receiver parameters
                        parameter.If( p => !IsReceiverParameter( p ) ).AddRule( regularParameterEligibilityInput );
                    } );

            var parameterEligibilityOutput =
                CreateRule<IParameter>(
                    parameter =>
                    {
                        parameter.If( IsReceiverParameter ).AddRule( receiverParameterEligibilityOutput );
                        parameter.If( p => !IsReceiverParameter( p ) ).AddRule( regularParameterEligibilityOutput );
                    } );

            var parameterEligibilityBoth =
                CreateRule<IParameter>(
                    parameter =>
                    {
                        parameter.If( IsReceiverParameter ).AddRule( receiverParameterEligibilityBoth );
                        parameter.If( p => !IsReceiverParameter( p ) ).AddRule( regularParameterEligibilityBoth );
                    } );

            var parameterEligibilityDefault =
                CreateRule<IParameter>(
                    parameter =>
                    {
                        parameter.If( IsReceiverParameter ).AddRule( receiverParameterEligibilityDefault );
                        parameter.If( p => !IsReceiverParameter( p ) ).AddRule( regularParameterEligibilityDefault );
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