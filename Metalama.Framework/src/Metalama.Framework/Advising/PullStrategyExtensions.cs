// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime;
using System.Linq;

namespace Metalama.Framework.Advising;

/// <summary>
/// Extension methods used in conjunction with <see cref="IPullStrategy"/>.
/// </summary>
[CompileTime]
public static class PullStrategyExtensions
{
    /// <summary>
    /// Returns <c>true</c> if the given member is an aspect-generated forwarding constructor, i.e. a compile-time stub
    /// that preserves the pre-mutation signature of a source constructor and chains, via <c>: this(...)</c>, to the
    /// mutated constructor. Such constructors are marked with <see cref="AspectGeneratedForwardingConstructorAttribute"/>.
    /// </summary>
    /// <remarks>
    /// Aspect authors implementing a custom <see cref="IPullStrategy"/> use this method from
    /// <see cref="IPullStrategy.GetPullAction"/> to detect whether the target is a forwarding constructor (as opposed to a regular chained constructor),
    /// so they can return a <see cref="PullAction.UseExpression"/>, <see cref="PullAction.UseConstant"/>,
    /// or <see cref="PullAction.UseExistingParameter"/> instead of the normal <see cref="PullAction.IntroduceParameterAndPull"/> cascade.
    /// </remarks>
    public static bool IsAspectGeneratedForwarder( this IHasParameters member )
        => member is IConstructor && member.Attributes.OfAttributeType( typeof(AspectGeneratedForwardingConstructorAttribute) ).Any();
}
