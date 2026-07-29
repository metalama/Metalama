// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Middle1;
using Middle2;

namespace Consumer;

/// <summary>
/// Derives from both base classes, so that the compile-time closure of this project contains the compile-time
/// projects of both <c>Middle1</c> and <c>Middle2</c>, and therefore both versions of <c>Contract</c>.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        System.Console.WriteLine( new Derived1().GetMessage1() );
        System.Console.WriteLine( new Derived2().GetMessage2() );
    }
}

public class Derived1 : BaseClass1 { }

public class Derived2 : BaseClass2 { }
