// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

#pragma warning disable CS8603 // Null-forgiving operator is intentional for this test

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.Return_NullForgiving;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Before" );

        // Null-forgiving operator on return - should still inline
        return meta.Proceed()!;
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private string? Method( string? a )
    {
        return a;
    }
}
