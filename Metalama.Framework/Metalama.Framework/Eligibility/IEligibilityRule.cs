// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Eligibility
{
    /// <summary>
    /// Encapsulates a predicate determining the eligibility of an object (typically a declaration or a type).
    /// </summary>
    /// <typeparam name="T">The type of object that the extension can be applied to.</typeparam>
    /// <seealso href="@eligibility"/>
    [CompileTime]
    public interface IEligibilityRule<in T>
        where T : class
    {
        /// <summary>
        /// Determines the eligibility of a given object for the current object extension.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        EligibleScenarios GetEligibility( T obj );

        /// <summary>
        /// Gets the justification for which the <see cref="GetEligibility"/> method returned anything else than <see cref="EligibleScenarios.All"/>. 
        /// </summary>
        /// <param name="requestedEligibility">The eligibility that was requested by the user, but denied.</param>
        /// <param name="describedObject">The object for which the eligibility was denied, plus its description.</param>
        /// <returns></returns>
        FormattableString? GetIneligibilityJustification( EligibleScenarios requestedEligibility, IDescribedObject<T> describedObject );
    }
}