// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Eligibility
{
    /// <summary>
    /// The non-generic base interface for <see cref="IEligibilityBuilder{T}"/>.
    /// </summary>
    /// <seealso cref="IEligibilityBuilder{T}"/>
    /// <seealso cref="IEligible{T}"/>
    /// <seealso cref="EligibilityExtensions"/>
    /// <seealso href="@eligibility"/> 
    [InternalImplement]
    [CompileTime]
    public interface IEligibilityBuilder
    {
        /// <summary>
        /// Gets the <see cref="EligibleScenarios"/> value that rules must return in case they evaluate negatively, i.e. what
        /// is the eligibility of the aspect on the target when the rule is <i>not</i> satisfied.  The default
        /// value of this property is <see cref="EligibleScenarios.None"/>, but it can be changed to anything
        /// using <see cref="EligibilityExtensions.ExceptForScenarios{T}"/>.
        /// </summary>
        EligibleScenarios IneligibleScenarios { get; }

        /// <summary>
        /// Builds an immutable rule from the current builder instance.
        /// </summary>
        /// <returns></returns>
        IEligibilityRule<IDeclaration> Build();
    }

    /// <summary>
    /// The argument of <see cref="IEligible{T}.BuildEligibility"/>. Allows aspect implementations to define eligibility requirements
    /// using extension methods from <see cref="EligibilityExtensions"/>.
    /// </summary>
    /// <typeparam name="T">Type of declaration being validated for eligibility.</typeparam>
    /// <remarks>
    /// <para>
    /// Use extension methods from <see cref="EligibilityExtensions"/> to add eligibility rules. Common methods include:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="EligibilityExtensions.MustNotBeStatic"/> - Requires instance members only</description></item>
    /// <item><description><see cref="EligibilityExtensions.MustNotBeAbstract"/> - Requires concrete implementations</description></item>
    /// <item><description><see cref="EligibilityExtensions.MustSatisfy"/> - Defines custom eligibility conditions</description></item>
    /// <item><description><see cref="EligibilityExtensions.DeclaringType"/> - Validates the declaring type</description></item>
    /// <item><description><see cref="EligibilityExtensions.ReturnType"/> - Validates method return types</description></item>
    /// <item><description><see cref="EligibilityExtensions.Parameter"/> - Validates method parameters</description></item>
    /// </list>
    /// <para>
    /// For complex scenarios, use <see cref="EligibilityExtensions.If"/> to apply conditional rules,
    /// or <see cref="EligibilityExtensions.MustSatisfyAny"/> to specify alternatives.
    /// </para>
    /// </remarks>
    /// <seealso cref="IEligibilityBuilder"/>
    /// <seealso cref="IEligible{T}"/>
    /// <seealso cref="EligibilityExtensions"/>
    /// <seealso cref="IEligibilityRule{T}"/>
    /// <seealso href="@eligibility"/> 
    public interface IEligibilityBuilder<out T> : IEligibilityBuilder
        where T : class
    {
        /// <summary>
        /// Adds a rule to the current builder.
        /// </summary>
        /// <param name="rule">The eligibility rule to add.</param>
        /// <remarks>
        /// For convenience, user code should use extension methods from <see cref="EligibilityExtensions"/>
        /// instead of calling this method directly. Methods like <see cref="EligibilityExtensions.MustNotBeStatic"/>,
        /// <see cref="EligibilityExtensions.MustSatisfy"/>, and others provide a more user-friendly API.
        /// </remarks>
        void AddRule( IEligibilityRule<T> rule );
    }
}