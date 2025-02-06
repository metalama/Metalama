// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Globalization;

namespace Metalama.Patterns.Memoization.AspectTests.Instance;

// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable EqualExpressionComparison
// ReSharper disable ReturnTypeCanBeNotNullable
#pragma warning disable CA2201

internal sealed class TheClass
{
    private int _counter;

    [Memoize]
    public string NonNullableMethod() => this._counter++.ToString( CultureInfo.InvariantCulture );

    [Memoize]
    public string? NullableMethod() => this._counter++.ToString( CultureInfo.InvariantCulture );

    [Memoize]
    public string NonNullableProperty => this._counter++.ToString( CultureInfo.InvariantCulture );

    [Memoize]
    public string? NullableProperty => this._counter++.ToString( CultureInfo.InvariantCulture );

    [Memoize]
    public Guid MethodReturnsStruct() => Guid.NewGuid();

    [Memoize]
    public Guid PropertyReturnsStruct => Guid.NewGuid();
}

internal static class Program
{
    public static void Main()
    {
        var o = new TheClass();

        if ( o.MethodReturnsStruct() != o.MethodReturnsStruct() )
        {
            throw new Exception();
        }

        if ( o.PropertyReturnsStruct != o.PropertyReturnsStruct )
        {
            throw new Exception();
        }

        if ( o.NonNullableMethod() != o.NonNullableMethod() )
        {
            throw new Exception();
        }

        if ( o.NullableMethod() != o.NullableMethod() )
        {
            throw new Exception();
        }

        if ( o.NonNullableProperty != o.NonNullableProperty )
        {
            throw new Exception();
        }

        if ( o.NullableProperty != o.NullableProperty )
        {
            throw new Exception();
        }

        Console.WriteLine( "Execution OK." );
    }
}