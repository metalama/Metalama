// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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