// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Reflection;

namespace Metalama.Patterns.Contracts.UnitTests;

public abstract class RangeContractTestsBase
{
    protected const double DoubleTolerance = 2.220446049250313E-16;
    protected const decimal DecimalTolerance = 0.0000000000000000000000000001M;

    protected static void AssertFails( Action<long?> method, long? longValue )
    {
        try
        {
            method( longValue );

            throw new AssertionFailedException( $"{method.GetMethodInfo().Name}( long?:{NullableToString( longValue )} ) did not fail." );
        }
        catch ( ArgumentOutOfRangeException ) { }
    }

    protected static void AssertFails( Action<ulong?> method, ulong? ulongValue )
    {
        try
        {
            method( ulongValue );

            throw new AssertionFailedException( $"{method.GetMethodInfo().Name}( ulong?:{NullableToString( ulongValue )} ) did not fail." );
        }
        catch ( ArgumentOutOfRangeException ) { }
    }

    protected static void AssertFails( Action<double?> method, double? doubleValue )
    {
        try
        {
            method( doubleValue );

            throw new AssertionFailedException( $"{method.GetMethodInfo().Name}( double?:{NullableToString( doubleValue )} ) did not fail." );
        }
        catch ( ArgumentOutOfRangeException ) { }
    }

    protected static void AssertFails( Action<decimal?> method, decimal? decimalValue )
    {
        try
        {
            method( decimalValue );

            throw new AssertionFailedException( $"{method.GetMethodInfo().Name}( decimal:{NullableToString( decimalValue )} ) did not fail." );
        }
        catch ( ArgumentOutOfRangeException ) { }
    }

    protected static void AssertFails(
        Action<long?, ulong?, double?, decimal?> method,
        long? longValue,
        ulong? ulongValue,
        double? doubleValue,
        decimal? decimalValue )
    {
        try
        {
            method( longValue, ulongValue, doubleValue, decimalValue );

            throw new AssertionFailedException(
                $"{method.GetMethodInfo().Name}( long?:{NullableToString( longValue )}, ulong?:{NullableToString( ulongValue )}, double?:{NullableToString( doubleValue )}, decimal?:{NullableToString( decimalValue )} ) did not fail." );
        }
        catch ( ArgumentOutOfRangeException ) { }
    }

    // Incorrect warning.
    private static string NullableToString( object? nullable ) => nullable?.ToString() ?? "null";
}