// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Globalization;

namespace Metalama.Patterns.Memoization.AspectTests.Static;

// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable EqualExpressionComparison
// ReSharper disable ReturnTypeCanBeNotNullable
#pragma warning disable CA2201

internal static class TheClass
{
    private static int _counter;

    [Memoize]
    public static string NonNullableMethod() => _counter++.ToString( CultureInfo.InvariantCulture );

    [Memoize]
    public static string? NullableMethod() => _counter++.ToString( CultureInfo.InvariantCulture );

    [Memoize]
    public static string NonNullableProperty => _counter++.ToString( CultureInfo.InvariantCulture );

    [Memoize]
    public static string? NullableProperty => _counter++.ToString( CultureInfo.InvariantCulture );

    [Memoize]
    public static Guid MethodReturnsStruct() => Guid.NewGuid();

    [Memoize]
    public static Guid PropertyReturnsStruct => Guid.NewGuid();
}

internal static class Program
{
    public static void Main()
    {
        if ( TheClass.MethodReturnsStruct() != TheClass.MethodReturnsStruct() )
        {
            throw new Exception();
        }

        if ( TheClass.PropertyReturnsStruct != TheClass.PropertyReturnsStruct )
        {
            throw new Exception();
        }

        if ( TheClass.NonNullableMethod() != TheClass.NonNullableMethod() )
        {
            throw new Exception();
        }

        if ( TheClass.NullableMethod() != TheClass.NullableMethod() )
        {
            throw new Exception();
        }

        if ( TheClass.NonNullableProperty != TheClass.NonNullableProperty )
        {
            throw new Exception();
        }

        if ( TheClass.NullableProperty != TheClass.NullableProperty )
        {
            throw new Exception();
        }

        Console.WriteLine( "Execution OK." );
    }
}