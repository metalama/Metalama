// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Operators.Declarative;

/*
 * Tests that using the declarative finalizer introduction produces an error.
 */

public class TestAttribute : TypeAspect
{
    [Introduce]
    public static TestAttribute operator -( TestAttribute t )
    {
        return t;
    }

    [Introduce]
    public static explicit operator string( TestAttribute t )
    {
        return t.ToString();
    }
}

// <target>
[Test]
internal class Target { }