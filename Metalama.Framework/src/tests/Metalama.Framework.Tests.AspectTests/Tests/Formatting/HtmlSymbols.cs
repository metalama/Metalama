// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @AddHtmlSymbols
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.HtmlSymbols;

// This test verifies that, with the AddHtmlSymbols option, each identifier gets the documentation id of the symbol it
// declares (data-def) or references (data-ref), and that an overriding member gets the id of the overridden member
// (data-overrides). Locals, parameters and type parameters have no documentation id, so they get a key that is unique in
// the document (data-local-def and data-local-ref).

public class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.Target.Method.Name );

        return meta.Proceed();
    }
}

public class Calculator<T>
{
    private readonly List<T> _values = new();

    public event EventHandler? Changed;

    public int Count => this._values.Count;

    [Log]
    public void Add( T value )
    {
        var values = this._values;
        values.Add( value );
        this.Changed?.Invoke( this, EventArgs.Empty );
    }

    public T First() => this._values.First();
}

// A positional parameter declares both a parameter and a property.
public record Point( int X, int Y );

public class Program
{
    public static void Main()
    {
        var calculator = new Calculator<int>();
        calculator.Add( 1 );
        Console.WriteLine( calculator.Count );

        var point = new Point( 1, 2 );
        Console.WriteLine( point.X );

        // Identifiers spelled like contextual keywords are ordinary symbols.
        var dynamic = 0;
        var @var = dynamic + 1;
        Console.WriteLine( nameof(point) + @var );
    }

    // The documentation id of a multidimensional array uses the compiler format, e.g. System.Int32[0:,0:].
    public static int Sum( int[,] values ) => values.Length;
}
