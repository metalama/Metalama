// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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