// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

// This is a modified copy of StrictlyLessOrGreaterThanPositiveValueTests.cs
// Keep the testing logic equal for all the copies!

namespace Metalama.Patterns.Contracts.UnitTests;

#pragma warning disable LAMA5006 // Intentionally with redundant checks.

public sealed class StrictlyLessOrGreaterThanZeroTests : RangeContractTestsBase
{
    private const long _longLimit = 0;
    private const ulong _ulongLimit = 0;
    private const double _doubleLimit = 0;

    private const double _doubleStep = double.Epsilon;
    private const decimal _decimalStep = DecimalTolerance;

    [Fact]
    public void TestMethodsWithStrictlyGreaterThanAspect_Success()
    {
        TestMethodsWithStrictlyGreaterThanAspect(
            _longLimit + 1,
            _ulongLimit + 1,
            _doubleLimit + _doubleStep,
            (decimal) _doubleLimit + _decimalStep );

        TestMethodsWithStrictlyGreaterThanAspect( 100, 100, 100, 100 );
        TestMethodsWithStrictlyGreaterThanAspect( long.MaxValue, ulong.MaxValue, double.MaxValue, decimal.MaxValue );
    }

    [Fact]
    public void TestMethodsWithStrictlyGreaterThanAspect_Failure()
    {
        AssertFails( TestMethodsWithStrictlyGreaterThanAspect, 0, 0, 0, 0 );
        AssertFails( TestMethodsWithStrictlyGreaterThanAspect, -100, null, -100, -100 );
        AssertFails( TestMethodsWithStrictlyGreaterThanAspect, long.MinValue, null, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithStrictlyLessThanAspect_Success()
    {
        TestMethodsWithStrictlyLessThanAspect(
            _longLimit - 1,
            null,
            _doubleLimit - _doubleStep,
            (decimal) _doubleLimit - _decimalStep );

        TestMethodsWithStrictlyLessThanAspect( -100, null, -100, -100 );
        TestMethodsWithStrictlyLessThanAspect( long.MinValue, ulong.MinValue, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithStrictlyLessThanAspect_Failure()
    {
        AssertFails( TestMethodsWithStrictlyLessThanAspect, 0, 0, 0, 0 );
        AssertFails( TestMethodsWithStrictlyLessThanAspect, 100, 100, 100, 100 );

        AssertFails(
            TestMethodsWithStrictlyLessThanAspect,
            long.MaxValue,
            ulong.MaxValue,
            double.MaxValue,
            decimal.MaxValue );
    }

    private static void TestMethodsWithStrictlyGreaterThanAspect(
        long? longValue,
        ulong? ulongValue,
        double? doubleValue,
        decimal? decimalValue )
    {
        MethodWithLongStrictlyGreaterThanLong( longValue );
        MethodWithUlongStrictlyGreaterThanLong( ulongValue );
        MethodWithDoubleStrictlyGreaterThanLong( doubleValue );
        MethodWithDecimalStrictlyGreaterThanLong( decimalValue );

        MethodWithLongStrictlyGreaterThanUlong( longValue );
        MethodWithUlongStrictlyGreaterThanUlong( ulongValue );
        MethodWithDoubleStrictlyGreaterThanUlong( doubleValue );
        MethodWithDecimalStrictlyGreaterThanUlong( decimalValue );

        MethodWithLongStrictlyGreaterThanDouble( longValue );
        MethodWithUlongStrictlyGreaterThanDouble( ulongValue );
        MethodWithDoubleStrictlyGreaterThanDouble( doubleValue );
        MethodWithDecimalStrictlyGreaterThanDouble( decimalValue );
    }

    private static void TestMethodsWithStrictlyLessThanAspect(
        long? longValue,
        ulong? ulongValue,
        double? doubleValue,
        decimal? decimalValue )
    {
        MethodWithLongStrictlyLessThanLong( longValue );
        MethodWithDoubleStrictlyLessThanLong( doubleValue );
        MethodWithDecimalStrictlyLessThanLong( decimalValue );

        MethodWithLongStrictlyLessThanUlong( longValue );
        MethodWithDoubleStrictlyLessThanUlong( doubleValue );
        MethodWithDecimalStrictlyLessThanUlong( decimalValue );

        MethodWithLongStrictlyLessThanDouble( longValue );
        MethodWithDoubleStrictlyLessThanDouble( doubleValue );
        MethodWithDecimalStrictlyLessThanDouble( decimalValue );
    }

    #region Long

    private static void MethodWithLongStrictlyGreaterThanLong( [StrictlyGreaterThan( _longLimit )] long? a ) { }

    private static void MethodWithUlongStrictlyGreaterThanLong( [StrictlyGreaterThan( _longLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyGreaterThanLong( [StrictlyGreaterThan( _longLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyGreaterThanLong( [StrictlyGreaterThan( _longLimit )] decimal? a ) { }

    private static void MethodWithLongStrictlyLessThanLong( [StrictlyLessThan( _longLimit )] long? a ) { }

    private static void MethodWithDoubleStrictlyLessThanLong( [StrictlyLessThan( _longLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyLessThanLong( [StrictlyLessThan( _longLimit )] decimal? a ) { }

    #endregion Long

    #region Ulong

    private static void MethodWithLongStrictlyGreaterThanUlong( [StrictlyGreaterThan( _ulongLimit )] long? a ) { }

    private static void MethodWithUlongStrictlyGreaterThanUlong( [StrictlyGreaterThan( _ulongLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyGreaterThanUlong( [StrictlyGreaterThan( _ulongLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyGreaterThanUlong( [StrictlyGreaterThan( _ulongLimit )] decimal? a ) { }

    private static void MethodWithLongStrictlyLessThanUlong( [StrictlyLessThan( _ulongLimit )] long? a ) { }

    private static void MethodWithDoubleStrictlyLessThanUlong( [StrictlyLessThan( _ulongLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyLessThanUlong( [StrictlyLessThan( _ulongLimit )] decimal? a ) { }

    #endregion Ulong

    #region Double

    private static void MethodWithLongStrictlyGreaterThanDouble( [StrictlyGreaterThan( _doubleLimit )] long? a ) { }

    private static void MethodWithUlongStrictlyGreaterThanDouble( [StrictlyGreaterThan( _doubleLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyGreaterThanDouble( [StrictlyGreaterThan( _doubleLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyGreaterThanDouble( [StrictlyGreaterThan( _doubleLimit )] decimal? a ) { }

    private static void MethodWithLongStrictlyLessThanDouble( [StrictlyLessThan( _doubleLimit )] long? a ) { }

    private static void MethodWithDoubleStrictlyLessThanDouble( [StrictlyLessThan( _doubleLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyLessThanDouble( [StrictlyLessThan( _doubleLimit )] decimal? a ) { }

    #endregion Double
}