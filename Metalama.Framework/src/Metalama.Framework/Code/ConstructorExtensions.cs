// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.RunTime;
using System.Linq;

namespace Metalama.Framework.Code;

/// <summary>
/// Provides extension methods on <see cref="IConstructor"/>.
/// </summary>
[CompileTime]
public static class ConstructorExtensions
{
    /// <summary>
    /// Returns <c>true</c> if the given constructor is a forwarding constructor emitted by the framework,
    /// i.e. a compile-time stub that preserves the pre-mutation signature of a source constructor and chains, via
    /// <c>: this(...)</c>, to the mutated constructor. Such constructors are marked with
    /// <see cref="SourceCompatibilityConstructorAttribute"/>.
    /// </summary>
    /// <remarks>
    /// Aspect authors implementing a custom <see cref="IPullStrategy"/> use this method from
    /// <see cref="IPullStrategy.GetPullAction"/> to detect whether the target is a forwarding constructor
    /// (as opposed to a regular chained constructor), so they can return a <see cref="PullAction.UseExpression"/>,
    /// <see cref="PullAction.UseConstant"/>, or <see cref="PullAction.UseExistingParameter"/> instead of the normal
    /// <see cref="PullAction.IntroduceParameterAndPull"/> cascade.
    /// </remarks>
    public static bool IsSourceCompatibilityConstructor( this IConstructor constructor )
        => constructor.Attributes.OfAttributeType( typeof(SourceCompatibilityConstructorAttribute) ).Any();
}
