// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug1125;

// Bug: Compile-time control flow does not stop after a `return` instruction,
// causing NullReferenceException when accessing members of null references.

internal class ReturnDoesNotStopCompileTimeFlowAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        // This will always be null because the predicate always returns false.
        var prop = meta.Target.Type.Properties.SingleOrDefault( x => false );

        if ( prop == null )
        {
            return null;
            // The compile-time flow does not stop here, but continues.
        }

        // NullReferenceException at compile time because prop is null.
        return prop.Value;
    }
}

internal class TargetCode
{
    public string Property { get; set; }

    // <target>
    [ReturnDoesNotStopCompileTimeFlowAspect]
    private object? Method() => null;
}
