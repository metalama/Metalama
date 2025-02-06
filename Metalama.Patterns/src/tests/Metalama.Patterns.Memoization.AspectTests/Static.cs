// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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