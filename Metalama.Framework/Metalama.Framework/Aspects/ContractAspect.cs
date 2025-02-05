// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base aspect that can validate or change the value of fields, properties, indexers, and parameters.
    /// </summary>
    /// <remarks>
    /// <para>A contract aspect can apply to the input or output data flow, or to both data flows, according to the <see cref="ContractDirection"/> value
    /// passed to the constructor. Since the current class does not know the value of this parameter before it is instantiated, this class cannot
    /// set the eligibility conditions using the <see cref="BuildEligibility(IEligibilityBuilder{IFieldOrPropertyOrIndexer})"/> method.
    /// If a derived class targets a specific <see cref="ContractDirection"/> (i.e. if the choice is not left to the user),
    /// its implementation of <see cref="BuildEligibility(IEligibilityBuilder{IFieldOrPropertyOrIndexer})"/>
    /// can call <see cref="BuildEligibilityForDirection(IEligibilityBuilder{IFieldOrPropertyOrIndexer},ContractDirection)"/>
    /// methods. This means that eligibility can be checked upfront by the IDE before suggesting the code actions.
    /// </para>
    /// <para>
    /// In any case, this aspect verifies the eligibility of the target with respect to the specific <see cref="ContractDirection"/> and target declaration. This verification
    /// cannot be skipped.
    /// </para>
    /// </remarks>
    [AttributeUsage( AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property )]
    [Layers( BuildLayer )]
    [Inheritable]
    public abstract partial class ContractAspect : Aspect, IAspect<IParameter>, IAspect<IFieldOrPropertyOrIndexer>
    {
        // Build after the default null-named layer so that other aspects can first inspect applications of ContractAspect-derived aspects
        // and then request redirection before the build layer.

        // ReSharper disable once MemberCanBePrivate.Global
        public const string BuildLayer = "Build";

        private readonly ContractDirection _direction;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractAspect"/> class.
        /// </summary>
        /// <param name="direction">The direction of the data flow (<see cref="ContractDirection.Input"/>,  <see cref="ContractDirection.Output"/> or <see cref="ContractDirection.Both"/>)
        /// to which this contract applies. See the <see cref="ContractDirection"/> for details.</param>
        [Obsolete( "Specifying the direction in the constructor is obsolete. Override the GetDirection method." )]
        protected ContractAspect( ContractDirection direction )
        {
            this._direction = direction;
        }

        protected ContractAspect()
        {
            this._direction = ContractDirection.Default;
        }

        /// <summary>
        /// Gets or sets the direction of the data flow (<see cref="ContractDirection.Input"/>,  <see cref="ContractDirection.Output"/> or <see cref="ContractDirection.Both"/>)
        /// to which this contract applies, as defined by the current aspect. This method returns <see cref="ContractDirection.Default"/> by default. When this method returns <see cref="ContractDirection.Default"/>,
        /// the actual direction is determined according to the characteristics of the target declaration.
        /// </summary>
        /// <remarks>
        /// In general, it is the responsibility of the <i>author</i> of the aspect, and not of its <i>user</i>, to define the eligible directions of a contract.
        /// However, the aspect's author can opt to allow users to define the contract direction by exposing this property as <c>public</c> in derived classes.
        /// </remarks>
        [PublicAPI]
        protected virtual ContractDirection GetDefinedDirection( IAspectBuilder builder ) => this._direction;

        /// <summary>
        /// Gets the actual direction of the contract given the direction returned by <see cref="GetDefinedDirection"/>, after resolving the <see cref="ContractDirection.Default"/>
        /// value according to the characteristics of the target declaration, and after taking predecessors and secondary instances into account. The implementation of this method
        /// may return <see cref="ContractDirection.None"/> to skip the aspect.
        /// </summary>
        [PublicAPI]
        protected virtual ContractDirection GetActualDirection( IAspectBuilder builder, ContractDirection direction ) => direction;

        private ContractDirection GetEffectiveDirection( IAspectBuilder aspectBuilder )
        {
            var predecessors = aspectBuilder.AspectInstance.Predecessors;
            var isInherited = !predecessors.IsDefaultOrEmpty && predecessors[0].Kind == AspectPredecessorKind.Inherited;

            var direction = this.GetDefinedDirection( aspectBuilder );

            if ( direction == ContractDirection.Default )
            {
                // If the contract was inherited, we need to resolve the ContractDirection.Default value based on the base declaration.
                // Indeed, assuming the aspect is applied to a get-only property of an interface, the intent is to validate the getter's return value.
                // However, when a class implements this interface, it could implement the property with an automatic property, which would then
                // have an implicit setter, and this would change the interpretation of the default behavior.

                IDeclaration baseDeclaration;

                if ( aspectBuilder.Target.DeclarationKind is DeclarationKind.Property or DeclarationKind.Indexer && isInherited )
                {
                    baseDeclaration = predecessors[0].Instance.TargetDeclaration.GetTarget( aspectBuilder.Target.Compilation );
                }
                else
                {
                    baseDeclaration = aspectBuilder.Target;
                }

                direction = ContractAspectHelper.GetEffectiveDirection( direction, baseDeclaration );
            }

            // We then need to restrict the direction based on the target declaration.
            // For example, a read-write base property with a read-only override needs to have the input direction removed.
            // But do this only for inherited contracts, so that invalid direction is still an error otherwise.
            if ( isInherited )
            {
                direction = direction.Restrict( ContractAspectHelper.GetPossibleDirection( aspectBuilder.Target ) );
            }

            // Combine secondary instances if any.
            foreach ( var instance in aspectBuilder.AspectInstance.SecondaryInstances )
            {
                direction = direction.CombineWith( ((ContractAspect) instance.Aspect).GetDefinedDirection( aspectBuilder ) );
            }

            return this.GetActualDirection( aspectBuilder, direction );
        }

        private static IReadOnlyCollection<IParameter>? GetValidatedDistinctProxyParametersForRedirection(
            IEnumerable<RedirectToProxyParameterAnnotation> annotations,
            IType targetType )
        {
            // Avoid performance hit for the very common case that there are no applicable annotations.

            using var iter = annotations.GetEnumerator();

            if ( !iter.MoveNext() )
            {
                return null;
            }

            // Then very common that there is only a single applicable annotation.

            var first = iter.Current;

            if ( !iter.MoveNext() )
            {
                // ReSharper disable once RedundantSuppressNullableWarningExpression
                return ParameterIsValid( first!.Parameter, targetType ) ? new[] { first.Parameter } : null;
            }

            var distinctByParameter = new HashSet<IParameter>();

            // ReSharper disable once RedundantSuppressNullableWarningExpression
            AddIfValid( distinctByParameter, first!.Parameter, targetType );

            // ReSharper disable once RedundantSuppressNullableWarningExpression
            AddIfValid( distinctByParameter, iter.Current!.Parameter, targetType );

            while ( iter.MoveNext() )
            {
                AddIfValid( distinctByParameter, iter.Current?.Parameter, targetType );
            }

            return distinctByParameter;

            static void AddIfValid( HashSet<IParameter> set, IParameter? parameter, IType expectedType )
            {
                if ( ParameterIsValid( parameter, expectedType ) )
                {
                    set.Add( parameter );
                }
            }

            static bool ParameterIsValid( [NotNullWhen( true )] IParameter? parameter, IType expectedType )
            {
                if ( parameter == null )
                {
                    return false;
                }

                var isValid = parameter.Type.Equals( expectedType );

                if ( !isValid )
                {
                    // TODO: How best to report invalid parameters?
                    // Invalid parameters would be caused by faulty logic in other aspects (which are expected to be in-house maintained), and
                    // the user can't take any action to fix this.

                    throw new InvalidOperationException(
                        "The type of " + nameof(RedirectToProxyParameterAnnotation) + "." + nameof(RedirectToProxyParameterAnnotation.Parameter)
                        + " does not match the type of the target of the " + nameof(ContractAspect) + "." );
                }

                return isValid;
            }
        }

        // TODO: Consider adding protected virtual RedirectionStrategy GetRedirectionStrategy( builder, IParameter proxyParameter ) so that derived types can opt out of redirection if they don't support it.
        // RedirectionStrategy could be { Redirect, Skip, Fail }

        void IAspect<IFieldOrPropertyOrIndexer>.BuildAspect( IAspectBuilder<IFieldOrPropertyOrIndexer> builder )
        {
            if ( builder.Layer != BuildLayer )
            {
                return;
            }

            var redirectToParameters = GetValidatedDistinctProxyParametersForRedirection(
                builder.Target.Enhancements().GetAnnotations<RedirectToProxyParameterAnnotation>(),
                builder.Target.Type );

            if ( redirectToParameters is { Count: > 0 } )
            {
                foreach ( var parameter in redirectToParameters )
                {
                    // TODO: Use AssertNotNull() extension method instead of `throw` if it can be made accessible.

                    this.BuildAspect(
                        ((IAspectBuilder) builder).With(
                            parameter.ForCompilation( builder.Target.Compilation ) ?? throw new InvalidOperationException( "Assertion failed." ) ) );
                }
            }
            else
            {
                this.BuildAspect( builder );
            }
        }

        // ReSharper disable once MemberCanBeProtected.Global
        public virtual void BuildAspect( IAspectBuilder<IFieldOrPropertyOrIndexer> builder )
        {
            var direction = this.GetEffectiveDirection( builder );

            if ( direction == ContractDirection.None )
            {
                builder.SkipAspect();

                return;
            }

            var eligibilityRule = EligibilityRuleFactory.GetContractAdviceEligibilityRule( direction );

            if ( !builder.VerifyEligibility( eligibilityRule ) )
            {
                // The aspect cannot be applied, but errors have been reported by the CheckEligibility method.
                return;
            }

            builder.AddContract( nameof(this.Validate), direction );
        }

        void IAspect<IParameter>.BuildAspect( IAspectBuilder<IParameter> builder )
        {
            if ( builder.Layer != BuildLayer )
            {
                return;
            }

            var redirectToParameters = GetValidatedDistinctProxyParametersForRedirection(
                builder.Target.Enhancements().GetAnnotations<RedirectToProxyParameterAnnotation>(),
                builder.Target.Type );

            if ( redirectToParameters is { Count: > 0 } )
            {
                foreach ( var parameter in redirectToParameters )
                {
                    this.BuildAspect( builder.With( parameter ) );
                }
            }
            else
            {
                this.BuildAspect( builder );
            }
        }

        // ReSharper disable once MemberCanBeProtected.Global
        public virtual void BuildAspect( IAspectBuilder<IParameter> builder )
        {
            var direction = this.GetEffectiveDirection( builder );

            if ( direction == ContractDirection.None )
            {
                builder.SkipAspect();

                return;
            }

            var eligibilityRule = EligibilityRuleFactory.GetContractAdviceEligibilityRule( direction );

            if ( !builder.VerifyEligibility( eligibilityRule ) )
            {
                // The aspect cannot be applied, but errors have been reported by the CheckEligibility method.

                return;
            }

            // If the aspect is applied to a record positional parameter, add the contract to the corresponding property.
            var parameter = builder.Target;

            IProperty? property;

            if ( parameter.DeclaringMember is IConstructor { DeclaringType.TypeKind: TypeKind.RecordClass or TypeKind.RecordStruct } constructor &&
                 (property = constructor.DeclaringType.Properties.OfName( builder.Target.Name ).SingleOrDefault()) != null )
            {
                builder.Advice.AddContract( property, nameof(this.Validate), direction );
            }
            else
            {
                builder.Advice.AddContract( parameter, nameof(this.Validate), direction );
            }
        }

        public virtual void BuildEligibility( IEligibilityBuilder<IFieldOrPropertyOrIndexer> builder )
        {
            // We don't know the actual direction yet, but we can apply common eligibility rules.
            BuildEligibilityForDirection( builder, ContractDirection.Default );
        }

        /// <summary>
        /// Populates the <see cref="IEligibilityBuilder"/> for a field, property or indexer when the <see cref="ContractDirection"/> is known.
        /// </summary>
        [PublicAPI]
        protected static void BuildEligibilityForDirection( IEligibilityBuilder<IFieldOrPropertyOrIndexer> builder, ContractDirection direction )
        {
            builder.AddRule( EligibilityRuleFactory.GetContractAdviceEligibilityRule( direction ) );
        }

        /// <summary>
        /// Populates the <see cref="IEligibilityBuilder"/> for a parameter when the <see cref="ContractDirection"/> is known.
        /// </summary>
        [PublicAPI]
        protected static void BuildEligibilityForDirection( IEligibilityBuilder<IParameter> builder, ContractDirection direction )
        {
            builder.AddRule( EligibilityRuleFactory.GetContractAdviceEligibilityRule( direction ) );
        }

        public virtual void BuildEligibility( IEligibilityBuilder<IParameter> builder )
        {
            // We don't know the actual direction yet, but we can apply common eligibility rules.
            BuildEligibilityForDirection( builder, ContractDirection.Default );
        }

        [Template]
        public abstract void Validate( dynamic? value );

        /// <summary>
        /// Redirects validation logic of <see cref="ContractAspect"/> from the specified property to the specified parameter.
        /// </summary>
        /// <param name="aspectBuilder">Current aspect builder.</param>
        /// <param name="sourceTarget">A declaration to redirect the validation logic from.</param>
        /// <param name="targetParameter">A parameter to redirect the validation logic to.</param>
        /// <remarks>
        /// <para>
        /// This call will only redirect validation logic of contracts applied after the current aspect. 
        /// Contracts applied before the current aspect will not be affected.
        /// </para>
        /// <para>
        /// If an aspect needs to see the contract aspect instances and redirect their validation logic at the same time, 
        /// it should be applied after the default layer of <see cref="ContractAspect"/> and before the layer that applies the contract logic, i.e. <see cref="ContractAspect.BuildLayer"/>.
        /// </para>
        /// </remarks>
        public static void RedirectContracts( IAspectBuilder aspectBuilder, IFieldOrPropertyOrIndexer sourceTarget, IParameter targetParameter )
        {
            aspectBuilder.Advice.AddAnnotation( sourceTarget, new RedirectToProxyParameterAnnotation( targetParameter ) );
        }
    }
}