// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Advising;

/// <summary>
/// Represents the result of the <c>Introduce*</c> methods of the <see cref="IAdviceFactory"/> interface.
/// </summary>
public interface IIntroductionAdviceResult<out T> : IAdviceResult, IAdviser<T>
    where T : class, IDeclaration
{
    /// <summary>
    /// Gets the introduced or overridden declaration. This returns the same value as the <see cref="IAdviser{T}.Target"/>
    /// property.
    /// </summary>
    T Declaration { get; }

    /// <summary>
    /// Gets the declaration that was in conflict, if the outcome is <see cref="AdviceOutcome.Error"/>. The member may be of a different kind that <see cref="Declaration"/>. 
    /// </summary>
    IDeclaration ConflictingDeclaration { get; }
}