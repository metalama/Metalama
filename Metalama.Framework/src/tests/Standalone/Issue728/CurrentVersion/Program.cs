// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Regression test for #728: TransitiveAspectSource throws NullReferenceException when aspects
// are coming from an assembly referencing an older Metalama version.
// This class inherits from IBaseInterface (defined in OldVersion with an older Metalama)
// and should successfully build with the current Metalama version.

internal partial class DerivedClass : IBaseInterface;

internal class Program
{
    public static void Main()
    {
        System.Console.WriteLine( "Cross-version transitive aspect test passed." );
    }
}
