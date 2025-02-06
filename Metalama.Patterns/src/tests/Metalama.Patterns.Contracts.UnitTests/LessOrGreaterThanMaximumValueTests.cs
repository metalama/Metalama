// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

// This is a modified copy of LessOrGreaterThanPositiveValueTests.cs
// Keep the testing logic equal for all the copies!

#pragma warning disable IDE0004  // Remove Unnecessary Cast: in this problem domain, explicit casts add clarity.
#pragma warning disable LAMA5006 // Intentionally with redundant checks.

// Resharper disable RedundantCast

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class LessOrGreaterThanMaximumValueTests : RangeContractTestsBase
{
    private const long _longLimit = long.MaxValue;
    private const ulong _ulongLimit = ulong.MaxValue;
    private const double _doubleLimit = double.MaxValue;
    private const decimal _decimalStep = decimal.MaxValue * DecimalTolerance;

    // This has to be double because decimal is not allowed as attribute constructor value.
    // Loss of precision is a consequence.
    private const double _decimalLimit = (double) decimal.MaxValue / (1 + DoubleTolerance);

    private static readonly double _doubleStep = Utilities.FloatingPointHelper.GetDoubleStep( _doubleLimit );

    [Fact]
    public void TestMethodsWithGreaterThanAspect_Success()
        => TestMethodsWithGreaterThanAspect( long.MaxValue, ulong.MaxValue, double.MaxValue, decimal.MaxValue );

    [Fact]
    public void TestMethodsWithGreaterThanAspect_Failure()
    {
        AssertFails(
            TestMethodsWithGreaterThanAspect,
            _longLimit - 1,
            _ulongLimit - 1,
            _doubleLimit - _doubleStep,
            (decimal) _decimalLimit - _decimalStep );

        AssertFails(
            TestMethodsWithGreaterThanAspect,
            _longLimit / 2,
            _ulongLimit / 2,
            _doubleLimit / 2,
            (decimal) _decimalLimit / 2 );

        AssertFails( TestMethodsWithGreaterThanAspect, 0, 0, 0, 0 );

        AssertFails(
            TestMethodsWithGreaterThanAspect,
            _longLimit / -2,
            null,
            _doubleLimit / -2,
            (decimal) _decimalLimit / -2 );

        AssertFails(
            TestMethodsWithGreaterThanAspect,
            long.MinValue,
            ulong.MinValue,
            double.MinValue,
            decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithLessThanAspect_Success()
    {
        MethodWithLongLessThanLong( long.MaxValue );
        MethodWithUlongLessThanLong( long.MaxValue );
        MethodWithDoubleLessThanLong( long.MaxValue );
        MethodWithDecimalLessThanLong( long.MaxValue );

        MethodWithLongLessThanUlong( long.MaxValue );
        MethodWithUlongLessThanUlong( ulong.MaxValue );
        MethodWithDoubleLessThanUlong( ulong.MaxValue );
        MethodWithDecimalLessThanUlong( ulong.MaxValue );

        MethodWithLongLessThanDouble( long.MaxValue );
        MethodWithUlongLessThanDouble( ulong.MaxValue );
        MethodWithDoubleLessThanDouble( double.MaxValue );
        MethodWithDecimalLessThanDouble( (decimal) _decimalLimit );

        TestMethodsWithLessThanAspect( 0, 0, 0, 0 );
        TestMethodsWithLessThanAspect( long.MinValue, ulong.MinValue, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithLessThanAspect_Failure()
    {
        AssertFails( MethodWithUlongLessThanLong, (ulong) long.MaxValue + 1 );
        AssertFails( MethodWithUlongLessThanLong, ulong.MaxValue );

        AssertFails( MethodWithDoubleLessThanLong, (double) long.MaxValue + _doubleStep );
        AssertFails( MethodWithDoubleLessThanLong, double.MaxValue );

        AssertFails( MethodWithDecimalLessThanLong, (decimal) long.MaxValue + _decimalStep );
        AssertFails( MethodWithDecimalLessThanLong, decimal.MaxValue );

        AssertFails( MethodWithDoubleLessThanUlong, (double) ulong.MaxValue + _doubleStep );
        AssertFails( MethodWithDoubleLessThanUlong, double.MaxValue );

        AssertFails( MethodWithDecimalLessThanUlong, (decimal) ulong.MaxValue + _decimalStep );
        AssertFails( MethodWithDecimalLessThanUlong, decimal.MaxValue );
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

        MethodWithLongGreaterThanUlong( longValue );
        MethodWithUlongGreaterThanUlong( ulongValue );
        MethodWithDoubleGreaterThanUlong( doubleValue );
        MethodWithDecimalGreaterThanUlong( decimalValue );

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

        MethodWithLongLessThanUlong( longValue );
        MethodWithUlongLessThanUlong( ulongValue );
        MethodWithDoubleLessThanUlong( doubleValue );
        MethodWithDecimalLessThanUlong( decimalValue );

        MethodWithLongLessThanDouble( longValue );
        MethodWithUlongLessThanDouble( ulongValue );
        MethodWithDoubleLessThanDouble( doubleValue );
        MethodWithDecimalLessThanDouble( decimalValue );
    }

    #region Long

    private static void MethodWithLongGreaterThanLong( [GreaterThan( _longLimit )] long? a ) { }

    private static void MethodWithUlongGreaterThanLong( [GreaterThan( _longLimit )] ulong? a ) { }

    private static void MethodWithDoubleGreaterThanLong( [GreaterThan( _longLimit )] double? a ) { }

    private static void MethodWithDecimalGreaterThanLong( [GreaterThan( _longLimit )] decimal? a ) { }

    private static void MethodWithLongLessThanLong( [LessThan( _longLimit )] long? a ) { }

    private static void MethodWithUlongLessThanLong( [LessThan( _longLimit )] ulong? a ) { }

    private static void MethodWithDoubleLessThanLong( [LessThan( _longLimit )] double? a ) { }

    private static void MethodWithDecimalLessThanLong( [LessThan( _longLimit )] decimal? a ) { }

    #endregion Long

    #region Ulong

    // Cannot use ulongLimit by design. Covered by build test.
    private static void MethodWithLongGreaterThanUlong( [GreaterThan( _longLimit )] long? a ) { }

    private static void MethodWithUlongGreaterThanUlong( [GreaterThan( _ulongLimit )] ulong? a ) { }

    private static void MethodWithDoubleGreaterThanUlong( [GreaterThan( _ulongLimit )] double? a ) { }

    private static void MethodWithDecimalGreaterThanUlong( [GreaterThan( _ulongLimit )] decimal? a ) { }

    private static void MethodWithLongLessThanUlong( [LessThan( _ulongLimit )] long? a ) { }

    private static void MethodWithUlongLessThanUlong( [LessThan( _ulongLimit )] ulong? a ) { }

    private static void MethodWithDoubleLessThanUlong( [LessThan( _ulongLimit )] double? a ) { }

    private static void MethodWithDecimalLessThanUlong( [LessThan( _ulongLimit )] decimal? a ) { }

    #endregion Ulong

    #region Double

    // Cannot use doubleLimit by design. Covered by build test.
    private static void MethodWithLongGreaterThanDouble( [GreaterThan( (double) _longLimit )] long? a ) { }

    // Cannot use doubleLimit by design. Covered by build test.
    private static void MethodWithUlongGreaterThanDouble( [GreaterThan( (double) _ulongLimit )] ulong? a ) { }

    private static void MethodWithDoubleGreaterThanDouble( [GreaterThan( _doubleLimit )] double? a ) { }

    // Cannot use doubleLimit by design. Covered by build test.
    private static void MethodWithDecimalGreaterThanDouble( [GreaterThan( (double) _decimalLimit )] decimal? a ) { }

    private static void MethodWithLongLessThanDouble( [LessThan( _doubleLimit )] long? a ) { }

    private static void MethodWithUlongLessThanDouble( [LessThan( _doubleLimit )] ulong? a ) { }

    private static void MethodWithDoubleLessThanDouble( [LessThan( _doubleLimit )] double? a ) { }

    private static void MethodWithDecimalLessThanDouble( [LessThan( _doubleLimit )] decimal? a ) { }

    #endregion Double
}