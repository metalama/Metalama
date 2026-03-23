// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable CS0078 // The 'l' suffix is easily confused with the digit '1'

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.LiteralSourceText;

public class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        // Direct run-time literals should preserve source text.
        Console.WriteLine( 0b11111111 );
        Console.WriteLine( 0xff );
        Console.WriteLine( 42l );
        Console.WriteLine( 42L );
        Console.WriteLine( 42u );
        Console.WriteLine( 42U );
        Console.WriteLine( 42ul );
        Console.WriteLine( 42lu );
        Console.WriteLine( 42.0f );
        Console.WriteLine( 42.0E-13f );
        Console.WriteLine( 42.0d );
        Console.WriteLine( 42.0E-13d );
        Console.WriteLine( 42.0m );
        Console.WriteLine( 42.0E-13m );
        Console.WriteLine( 1_000_000 );
        Console.WriteLine( 0x00_ff_ff );
        Console.WriteLine( 0b0000_1111_0000 );

        // Compile-time literals via meta.CompileTime should also preserve source text.
        var ctHex = meta.CompileTime( 0xff );
        Console.WriteLine( ctHex );

        return meta.Proceed();
    }
}

public class TargetClass
{
    // <target>
    [TestAspect]
    public void Method() { }
}
