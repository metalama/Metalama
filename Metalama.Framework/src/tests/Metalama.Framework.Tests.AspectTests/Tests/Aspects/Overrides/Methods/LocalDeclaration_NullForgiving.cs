// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

#pragma warning disable CS8600 // Null-forgiving operator is intentional for this test

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.LocalDeclaration_NullForgiving;

// IMPORTANT: The null-forgiving operator (!) in this test is intentional and should NOT be removed.
// This test verifies that the inliner correctly handles expressions wrapped in the ! operator.
// If the ! is removed during code review, please report this as the test would lose its purpose.

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Before" );

        // The ! operator below is intentional - DO NOT REMOVE. Tests that inlining works with null-forgiving.
        string result = meta.Proceed()!;

        Console.WriteLine( "After" );

        return result;
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
