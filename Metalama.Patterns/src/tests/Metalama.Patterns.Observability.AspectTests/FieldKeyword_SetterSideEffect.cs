// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

// ReSharper disable ConvertToAutoPropertyWithInitializer
// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace Metalama.Patterns.Observability.AspectTests.FieldKeyword_SetterSideEffect;

// <target>
[Observable]
public class FieldKeyword_SetterSideEffect
{
    // Semi-automatic property whose manual setter has a side effect (regression test for #1644).
    // The aspect must execute the original setter body, not just assign the raw value to the backing field.
    public int Value
    {
        get => field;
        set
        {
            field = value;
            this.SideEffect++;
        }
    }

    public int SideEffect { get; private set; }
}

internal static class Program
{
    public static void Main()
    {
        var foo = new FieldKeyword_SetterSideEffect();
        foo.Value = 42;

        // The original setter body (field = value; SideEffect++;) must run, so SideEffect must be 1.
        Console.WriteLine( $"Value={foo.Value} (expected 42)" );
        Console.WriteLine( $"SideEffect={foo.SideEffect} (expected 1)" );

        if ( foo.Value != 42 || foo.SideEffect != 1 )
        {
            throw new InvalidOperationException(
                $"The original setter body was not executed: Value={foo.Value} (expected 42), SideEffect={foo.SideEffect} (expected 1)." );
        }

        Console.WriteLine( "Execution OK." );
    }
}
