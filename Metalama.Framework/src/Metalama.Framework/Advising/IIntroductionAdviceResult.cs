// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Advising;

/// <summary>
/// Represents the result of introduction advice methods such as <see cref="AdviserExtensions.IntroduceMethod"/>,
/// <see cref="AdviserExtensions.IntroduceProperty(Metalama.Framework.Aspects.IAdviser{Metalama.Framework.Code.INamedType}, string, IntroductionScope, OverrideStrategy, System.Action{Metalama.Framework.Code.DeclarationBuilders.IPropertyBuilder}, object)">IntroduceProperty</see>,
/// <see cref="AdviserExtensions.IntroduceField(Metalama.Framework.Aspects.IAdviser{Metalama.Framework.Code.INamedType}, string, IntroductionScope, OverrideStrategy, System.Action{Metalama.Framework.Code.DeclarationBuilders.IFieldBuilder}, object)">IntroduceField</see>, or
/// <see cref="AdviserExtensions.IntroduceEvent(Metalama.Framework.Aspects.IAdviser{Metalama.Framework.Code.INamedType}, string, IntroductionScope, OverrideStrategy, System.Action{Metalama.Framework.Code.DeclarationBuilders.IEventBuilder}, object)">IntroduceEvent</see>.
/// </summary>
/// <remarks>
/// <para>
/// This interface also implements <see cref="IAdviser{T}"/>, which allows chaining additional advice on the introduced declaration.
/// </para>
/// </remarks>
/// <seealso cref="AdviceResultExtensions.TryGetDeclaration{T}"/>
/// <seealso cref="IAdviceResult"/>
/// <seealso cref="IAdviser{T}"/>
/// <seealso cref="AdviserExtensions"/>
/// <seealso cref="AdviceOutcome"/>
/// <seealso href="@introducing-members"/>
public interface IIntroductionAdviceResult<out T> : IAdviceResult, IAdviser<T>
    where T : class, IDeclaration
{
    /// <summary>
    /// Gets the introduced or overridden declaration. This returns the same value as the <see cref="IAdviser{T}.Target"/>
    /// property.
    /// </summary>
    /// <seealso cref="AdviceResultExtensions.TryGetDeclaration{T}"/>
    T Declaration { get; }

    /// <summary>
    /// Gets the declaration that was in conflict, if the outcome is <see cref="AdviceOutcome.Error"/>. The member may be of a different kind that <see cref="Declaration"/>. 
    /// </summary>
    IDeclaration ConflictingDeclaration { get; }
}