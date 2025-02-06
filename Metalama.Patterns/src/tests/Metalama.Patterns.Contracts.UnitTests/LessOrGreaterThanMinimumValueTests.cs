// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

// This is a modified copy of LessOrGreaterThanPositiveValueTests.cs
// Keep the testing logic equal for all the copies!

#pragma warning disable IDE0004  // Remove Unnecessary Cast: in this problem domain, explicit casts add clarity.
#pragma warning disable LAMA5006 // Intentionally with redundant checks.

// Resharper disable RedundantCast

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class LessOrGreaterThanMinimumValueTests : RangeContractTestsBase
{
    private const long _longLimit = long.MinValue;
    private const double _longAsDoubleLimit = -100;
    private const ulong _ulongLimit = 100;
    private const double _doubleLimit = double.MinValue;

    // This has to be double because decimal is not allowed as attribute constructor value.
    // Loss of precision is a consequence.
    private const double _decimalMin = (double) decimal.MinValue / (1 + DoubleTolerance);

    private static readonly double _doubleStep = Utilities.FloatingPointHelper.GetDoubleStep( _doubleLimit );
    private static readonly decimal _decimalStep = Math.Abs( decimal.MinValue ) * DecimalTolerance;

    [Fact]
    public void TestMethodsWithGreaterThanAspect_Success()
    {
        MethodWithLongGreaterThanLong( long.MinValue );
        MethodWithDoubleGreaterThanLong( long.MinValue );
        MethodWithDecimalGreaterThanLong( long.MinValue );

        MethodWithLongGreaterThanDouble( long.MinValue );
        MethodWithDoubleGreaterThanDouble( double.MinValue );
        MethodWithDecimalGreaterThanDouble( (decimal) _decimalMin );

        TestMethodsWithGreaterThanAspect( 0, 0, 0, 0 );
        TestMethodsWithGreaterThanAspect( long.MaxValue, ulong.MaxValue, double.MaxValue, decimal.MaxValue );
    }

    [Fact]
    public void TestMethodsWithGreaterThanAspect_Failure()
    {
        AssertFails( MethodWithDoubleGreaterThanLong, (double) long.MinValue - _doubleStep );
        AssertFails( MethodWithDoubleGreaterThanLong, double.MinValue );

        AssertFails( MethodWithDecimalGreaterThanLong, (decimal) long.MinValue - _decimalStep );
        AssertFails( MethodWithDecimalGreaterThanLong, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithLessThanAspect_Success()
    {
        MethodWithLongGreaterThanLong( long.MinValue );
        MethodWithDoubleGreaterThanLong( long.MinValue );
        MethodWithDecimalGreaterThanLong( long.MinValue );

        MethodWithLongGreaterThanDouble( long.MinValue );
        MethodWithDoubleGreaterThanDouble( double.MinValue );
        MethodWithDecimalGreaterThanDouble( (decimal) _decimalMin );
    }

    [Fact]
    public void TestMethodsWithLessThanAspect_Failure()
    {
        AssertFails( MethodWithLongLessThanLong, long.MinValue + 1 );
        AssertFails( MethodWithDoubleLessThanLong, (double) long.MinValue + _doubleStep );
        AssertFails( MethodWithDecimalLessThanLong, (decimal) long.MinValue + 1 );

        AssertFails( MethodWithLongLessThanDouble, (long) _longAsDoubleLimit + 1 );
        AssertFails( MethodWithDoubleLessThanDouble, double.MinValue + _doubleStep );
        AssertFails( MethodWithDecimalLessThanDouble, (decimal) _decimalMin + _decimalStep );

        AssertFails( TestMethodsWithLessThanAspect, 0, 0, 0, 0 );
        AssertFails( TestMethodsWithLessThanAspect, long.MaxValue, ulong.MaxValue, double.MaxValue, decimal.MaxValue );
    }

    private static void TestMethodsWithGreaterThanAspect(
        long? longValue,
        ulong? ulongValue,
        double? doubleValue,
        decimal? decimalValue )
    {
        MethodWithLongGreaterThanLong( longValue );
        MethodWithUlongGreaterThanLong( ulongValue );
        MethodWithDoubleGreaterThanLong( doubleValue );
        MethodWithDecimalGreaterThanLong( decimalValue );

        MethodWithLongGreaterThanDouble( longValue );
        MethodWithUlongGreaterThanDouble( ulongValue );
        MethodWithDoubleGreaterThanDouble( doubleValue );
        MethodWithDecimalGreaterThanDouble( decimalValue );
    }

    private static void TestMethodsWithLessThanAspect(
        long? longValue,
        ulong? ulongValue,
        double? doubleValue,
        decimal? decimalValue )
    {
        MethodWithLongLessThanLong( longValue );
        MethodWithUlongLessThanLong( ulongValue );
        MethodWithDoubleLessThanLong( doubleValue );
        MethodWithDecimalLessThanLong( decimalValue );

        MethodWithLongLessThanDouble( longValue );
        MethodWithDoubleLessThanDouble( doubleValue );
        MethodWithDecimalLessThanDouble( decimalValue );
    }

    #region Long

    private static void MethodWithLongGreaterThanLong( [GreaterThan( _longLimit )] long? a ) { }

    private static void MethodWithUlongGreaterThanLong( [GreaterThan( _longLimit )] ulong? a ) { }

    private static void MethodWithDoubleGreaterThanLong( [GreaterThan( _longLimit )] double? a ) { }

    private static void MethodWithDecimalGreaterThanLong( [GreaterThan( _longLimit )] decimal? a ) { }

    private static void MethodWithLongLessThanLong( [LessThan( _longLimit )] long? a ) { }

    private static void MethodWithUlongLessThanLong( [LessThan( _ulongLimit )] ulong? a ) { }

    private static void MethodWithDoubleLessThanLong( [LessThan( _longLimit )] double? a ) { }

    private static void MethodWithDecimalLessThanLong( [LessThan( _longLimit )] decimal? a ) { }

    #endregion Long

    #region Double

    private static void MethodWithLongGreaterThanDouble( [GreaterThan( _doubleLimit )] long? a ) { }

    private static void MethodWithUlongGreaterThanDouble( [GreaterThan( _doubleLimit )] ulong? a ) { }

    private static void MethodWithDoubleGreaterThanDouble( [GreaterThan( _doubleLimit )] double? a ) { }

    private static void MethodWithDecimalGreaterThanDouble( [GreaterThan( _doubleLimit )] decimal? a ) { }

    private static void MethodWithLongLessThanDouble( [LessThan( _longAsDoubleLimit )] long? a ) { }

    private static void MethodWithDoubleLessThanDouble( [LessThan( _doubleLimit )] double? a ) { }

    private static void MethodWithDecimalLessThanDouble( [LessThan( _decimalMin )] decimal? a ) { }

    #endregion Double
}