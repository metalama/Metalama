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
        // Compile-time literals serialized back to run-time code.
        var ctBin = meta.CompileTime( 0b11111111 );
        var ctHex = meta.CompileTime( 0xff );
        var ctLowerL = meta.CompileTime( 42l );
        var ctUpperL = meta.CompileTime( 42L );
        var ctLowerU = meta.CompileTime( 42u );
        var ctUpperU = meta.CompileTime( 42U );
        var ctUl = meta.CompileTime( 42ul );
        var ctLu = meta.CompileTime( 42lu );
        var ctFloat = meta.CompileTime( 42.0f );
        var ctFloatExp = meta.CompileTime( 42.0E-13f );
        var ctDouble = meta.CompileTime( 42.0d );
        var ctDoubleExp = meta.CompileTime( 42.0E-13d );
        var ctDecimal = meta.CompileTime( 42.0m );
        var ctDecimalExp = meta.CompileTime( 42.0E-13m );
        var ctIntUnderscores = meta.CompileTime( 1_000_000 );
        var ctHexUnderscores = meta.CompileTime( 0x00_ff_ff );
        var ctBinUnderscores = meta.CompileTime( 0b0000_1111_0000 );

        Console.WriteLine( ctBin );
        Console.WriteLine( ctHex );
        Console.WriteLine( ctLowerL );
        Console.WriteLine( ctUpperL );
        Console.WriteLine( ctLowerU );
        Console.WriteLine( ctUpperU );
        Console.WriteLine( ctUl );
        Console.WriteLine( ctLu );
        Console.WriteLine( ctFloat );
        Console.WriteLine( ctFloatExp );
        Console.WriteLine( ctDouble );
        Console.WriteLine( ctDoubleExp );
        Console.WriteLine( ctDecimal );
        Console.WriteLine( ctDecimalExp );
        Console.WriteLine( ctIntUnderscores );
        Console.WriteLine( ctHexUnderscores );
        Console.WriteLine( ctBinUnderscores );

        return meta.Proceed();
    }
}

public class TargetClass
{
    // <target>
    [TestAspect]
    public void Method() { }
}
