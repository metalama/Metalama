// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.Assignment_NullForgiving;

// IMPORTANT: The null-forgiving operator (!) in this test is intentional and should NOT be removed.
// This test verifies that the inliner correctly handles expressions wrapped in the ! operator.
// If the ! is removed during code review, please report this as the test would lose its purpose.

internal class Aspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get
        {
            Console.WriteLine( "Before" );

            // The ! operator below is intentional - DO NOT REMOVE. Tests that inlining works with null-forgiving.
            int result;
            result = meta.Proceed()!;

            Console.WriteLine( "After" );

            return result;
        }

        set => meta.Proceed();
    }
}

// <target>
internal class TargetCode
{
    private int _field;

    [Aspect]
    public int Property
    {
        get
        {
            return _field;
        }

        set
        {
            _field = value;
        }
    }
}
