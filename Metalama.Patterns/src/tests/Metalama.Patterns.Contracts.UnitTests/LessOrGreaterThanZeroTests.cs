// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

// This is a modified copy of LessOrGreaterThanPositiveValueTests.cs
// Keep the testing logic equal for all the copies!

#pragma warning disable LAMA5006 // Intentionally with redundant checks.

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class LessOrGreaterThanZeroTests : RangeContractTestsBase
{
    private const long _longLimit = 0;
    private const ulong _ulongLimit = 0;
    private const double _doubleLimit = 0;

    private const double _doubleStep = double.Epsilon;
    private const decimal _decimalStep = DecimalTolerance;

    [Fact]
    public void TestMethodsWithGreaterThanAspect_Success()
    {
        TestMethodsWithGreaterThanAspect( 0, 0, 0, 0 );
        TestMethodsWithGreaterThanAspect( 100, 100, 100, 100 );
        TestMethodsWithGreaterThanAspect( long.MaxValue, ulong.MaxValue, double.MaxValue, decimal.MaxValue );
    }

    [Fact]
    public void TestMethodsWithGreaterThanAspect_Failure()
    {
        AssertFails(
            TestMethodsWithGreaterThanAspect,
            _longLimit - 1,
            null,
            _doubleLimit - _doubleStep,
            (decimal) _doubleLimit - _decimalStep );

        AssertFails( TestMethodsWithGreaterThanAspect, -100, null, -100, -100 );
        AssertFails( TestMethodsWithGreaterThanAspect, long.MinValue, null, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithLessThanAspect_Success()
    {
        TestMethodsWithLessThanAspect( 0, 0, 0, 0 );
        TestMethodsWithLessThanAspect( -100, null, -100, -100 );
        TestMethodsWithLessThanAspect( long.MinValue, null, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithLessThanAspect_Failure()
    {
        AssertFails(
            TestMethodsWithLessThanAspect,
            _longLimit + 1,
            1,
            _doubleLimit + _doubleStep,
            (decimal) _doubleLimit + _decimalStep );

        AssertFails( TestMethodsWithLessThanAspect, 100, 100, 100, 100 );
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

    private static void MethodWithLongGreaterThanUlong( [GreaterThan( _ulongLimit )] long? a ) { }

    private static void MethodWithUlongGreaterThanUlong( [GreaterThan( _ulongLimit )] ulong? a ) { }

    private static void MethodWithDoubleGreaterThanUlong( [GreaterThan( _ulongLimit )] double? a ) { }

    private static void MethodWithDecimalGreaterThanUlong( [GreaterThan( _ulongLimit )] decimal? a ) { }

    private static void MethodWithLongLessThanUlong( [LessThan( _ulongLimit )] long? a ) { }

    private static void MethodWithUlongLessThanUlong( [LessThan( _ulongLimit )] ulong? a ) { }

    private static void MethodWithDoubleLessThanUlong( [LessThan( _ulongLimit )] double? a ) { }

    private static void MethodWithDecimalLessThanUlong( [LessThan( _ulongLimit )] decimal? a ) { }

    #endregion Ulong

    #region Double

    private static void MethodWithLongGreaterThanDouble( [GreaterThan( _doubleLimit )] long? a ) { }

    private static void MethodWithUlongGreaterThanDouble( [GreaterThan( _doubleLimit )] ulong? a ) { }

    private static void MethodWithDoubleGreaterThanDouble( [GreaterThan( _doubleLimit )] double? a ) { }

    private static void MethodWithDecimalGreaterThanDouble( [GreaterThan( _doubleLimit )] decimal? a ) { }

    private static void MethodWithLongLessThanDouble( [LessThan( _doubleLimit )] long? a ) { }

    private static void MethodWithUlongLessThanDouble( [LessThan( _doubleLimit )] ulong? a ) { }

    private static void MethodWithDoubleLessThanDouble( [LessThan( _doubleLimit )] double? a ) { }

    private static void MethodWithDecimalLessThanDouble( [LessThan( _doubleLimit )] decimal? a ) { }

    #endregion Double
}