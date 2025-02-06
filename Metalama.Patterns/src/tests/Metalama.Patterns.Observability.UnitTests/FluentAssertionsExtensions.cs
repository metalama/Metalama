// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using FluentAssertions;
using FluentAssertions.Collections;
using System.Diagnostics;

namespace Metalama.Patterns.Observability.UnitTests;

// ReSharper disable once UnusedType.Global
[DebuggerNonUserCode]
public static class FluentAssertionsExtensions
{
    // ReSharper disable once UnusedMember.Global

    /// <summary>
    /// Use as a drop-in replacement for <c>Equal</c> in code like <c>Should().Equal( "A1", "B2" )</c>
    /// where the order of elements is irrelevant. 
    /// </summary>
    public static AndConstraint<GenericCollectionAssertions<TExpectation>> BeEquivalentTo<TExpectation>(
        this GenericCollectionAssertions<IEnumerable<TExpectation>, TExpectation, GenericCollectionAssertions<TExpectation>> assertions,
        params TExpectation[] expectation )
    {
        return assertions.BeEquivalentTo( expectation );
    }
}