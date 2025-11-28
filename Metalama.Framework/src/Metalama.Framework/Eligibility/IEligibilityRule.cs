// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Eligibility
{
    /// <summary>
    /// Encapsulates a predicate determining the eligibility of an object (typically a declaration or a type) across different scenarios.
    /// </summary>
    /// <typeparam name="T">The type of object that the rule evaluates (typically an <see cref="IDeclaration"/> or derived type).</typeparam>
    /// <remarks>
    /// <para>
    /// User code typically does not implement this interface directly. Instead, use <see cref="EligibilityExtensions.MustSatisfy"/>
    /// or other extension methods from <see cref="EligibilityExtensions"/> to create rules declaratively.
    /// </para>
    /// <para>
    /// This interface is used internally by the eligibility system to evaluate whether a declaration meets the requirements
    /// for aspect application in various scenarios (direct application, inheritance, live templates).
    /// </para>
    /// </remarks>
    /// <seealso cref="EligibilityExtensions"/>
    /// <seealso cref="IEligibilityBuilder{T}"/>
    /// <seealso href="@eligibility"/>
    [CompileTime]
    public interface IEligibilityRule<in T>
        where T : class
    {
        /// <summary>
        /// Determines the eligibility of a given object for the aspect or option.
        /// </summary>
        /// <param name="obj">The object (typically a declaration) to evaluate for eligibility.</param>
        /// <returns>
        /// A combination of <see cref="EligibleScenarios"/> flags indicating in which scenarios the object is eligible.
        /// Returns <see cref="EligibleScenarios.All"/> if the object is eligible in all scenarios,
        /// <see cref="EligibleScenarios.None"/> if completely ineligible, or a combination of flags for partial eligibility.
        /// </returns>
        EligibleScenarios GetEligibility( T obj );

        /// <summary>
        /// Gets a human-readable explanation for why the <see cref="GetEligibility"/> method denied eligibility in a requested scenario.
        /// </summary>
        /// <param name="requestedEligibility">The eligibility scenario that was requested by the user but denied by this rule.</param>
        /// <param name="describedObject">The object for which eligibility was denied, along with its formatted description.</param>
        /// <returns>
        /// A <see cref="FormattableString"/> explaining why the object is not eligible, or <c>null</c> if no justification is needed.
        /// The string should describe what the object <em>must</em> be to satisfy the rule (e.g., "must not be static").
        /// </returns>
        FormattableString? GetIneligibilityJustification( EligibleScenarios requestedEligibility, IDescribedObject<T> describedObject );
    }
}