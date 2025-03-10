// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Eligibility
{
    /// <summary>
    /// The non-generic base interface for <see cref="IEligibilityBuilder{T}"/>.
    /// </summary>
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
    /// The argument of <see cref="IEligible{T}.BuildEligibility"/>. Allows the implementation to add requirements
    /// using methods of <see cref="EligibilityExtensions"/>.
    /// </summary>
    /// <typeparam name="T">Type of declaration.</typeparam>
    /// <seealso href="@eligibility"/>
    /// <seealso cref="EligibilityExtensions"/> 
    public interface IEligibilityBuilder<out T> : IEligibilityBuilder
        where T : class
    {
        /// <summary>
        /// Adds a rule to the current builder. For convenience, user code should use extension methods
        /// from <see cref="EligibilityExtensions"/>. 
        /// </summary>
        void AddRule( IEligibilityRule<T> rule );
    }
}