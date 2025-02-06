// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

// This is a modified copy of StrictlyLessOrGreaterThanPositiveValueTests.cs
// Keep the testing logic equal for all the copies!

#pragma warning disable IDE0004  // Remove Unnecessary Cast: in this problem domain, explicit casts add clarity.
#pragma warning disable LAMA5006 // Intentionally with redundant checks.

// Resharper disable RedundantCast

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class StrictlyLessOrGreaterThanMaximumValueTests : RangeContractTestsBase
{
    private const long _longLimit = long.MaxValue - 1;
    private const ulong _ulongLimit = ulong.MaxValue - 1;
    private const double _doubleLimit = double.MaxValue / (1 + DoubleTolerance);

    // This has to be double because decimal is not allowed as attribute constructor value.
    // Loss of precision is a consequence.
    private const double _decimalLimit = (double) decimal.MaxValue / (1 + DoubleTolerance);

    private const double _doubleStep = double.MaxValue - _doubleLimit;
    private const decimal _decimalStep = decimal.MaxValue - (decimal) _decimalLimit;

    [Fact]
    public void TestMethodsWithStrictlyGreaterThanAspect_Success()
        => TestMethodsWithStrictlyGreaterThanAspect( long.MaxValue, ulong.MaxValue, double.MaxValue, decimal.MaxValue );

    [Fact]
    public void TestMethodsWithStrictlyGreaterThanAspect_Failure()
    {
        AssertFails(
            TestMethodsWithStrictlyGreaterThanAspect,
            _longLimit,
            _ulongLimit,
            _doubleLimit,
            (decimal) _decimalLimit );

        AssertFails(
            TestMethodsWithStrictlyGreaterThanAspect,
            _longLimit / 2,
            _ulongLimit / 2,
            _doubleLimit / 2,
            (decimal) _decimalLimit / 2 );

        AssertFails( TestMethodsWithStrictlyGreaterThanAspect, 0, 0, 0, 0 );

        AssertFails(
            TestMethodsWithStrictlyGreaterThanAspect,
            _longLimit / -2,
            null,
            _doubleLimit / -2,
            (decimal) _decimalLimit / -2 );

        AssertFails(
            TestMethodsWithStrictlyGreaterThanAspect,
            long.MinValue,
            ulong.MinValue,
            double.MinValue,
            decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithStrictlyLessThanAspect_Success()
    {
        MethodWithLongStrictlyLessThanLong( _longLimit - 1 );
        MethodWithUlongStrictlyLessThanLong( (ulong) _longLimit - 1 );
        MethodWithDoubleStrictlyLessThanLong( (double) _longLimit - _doubleStep );
        MethodWithDecimalStrictlyLessThanLong( (decimal) _longLimit - _decimalStep );

        MethodWithLongStrictlyLessThanUlong( long.MaxValue );
        MethodWithUlongStrictlyLessThanUlong( _ulongLimit - 1 );
        MethodWithDoubleStrictlyLessThanUlong( (double) _ulongLimit - _doubleStep );
        MethodWithDecimalStrictlyLessThanUlong( (decimal) _ulongLimit - _decimalStep );

        MethodWithLongStrictlyLessThanDouble( long.MaxValue );
        MethodWithUlongStrictlyLessThanDouble( ulong.MaxValue );
        MethodWithDoubleStrictlyLessThanDouble( _doubleLimit - _doubleStep );
        MethodWithDecimalStrictlyLessThanDouble( (decimal) _decimalLimit - _decimalStep );

        TestMethodsWithStrictlyLessThanAspect( 0, 0, 0, 0 );
        TestMethodsWithStrictlyLessThanAspect( _longLimit / -2, null, _doubleLimit / -2, (decimal) _decimalLimit / -2 );
        TestMethodsWithStrictlyLessThanAspect( long.MinValue, ulong.MinValue, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithStrictlyLessThanAspect_Failure()
    {
        AssertFails(
            TestMethodsWithStrictlyLessThanAspect,
            _longLimit,
            _ulongLimit,
            _doubleLimit,
            (decimal) _decimalLimit );

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
        MethodWithUlongStrictlyLessThanLong( ulongValue );
        MethodWithDoubleStrictlyLessThanLong( doubleValue );
        MethodWithDecimalStrictlyLessThanLong( decimalValue );

        MethodWithLongStrictlyLessThanUlong( longValue );
        MethodWithUlongStrictlyLessThanUlong( ulongValue );
        MethodWithDoubleStrictlyLessThanUlong( doubleValue );
        MethodWithDecimalStrictlyLessThanUlong( decimalValue );

        MethodWithLongStrictlyLessThanDouble( longValue );
        MethodWithUlongStrictlyLessThanDouble( ulongValue );
        MethodWithDoubleStrictlyLessThanDouble( doubleValue );
        MethodWithDecimalStrictlyLessThanDouble( decimalValue );
    }

    #region Long

    private static void MethodWithLongStrictlyGreaterThanLong( [StrictlyGreaterThan( _longLimit )] long? a ) { }

    private static void MethodWithUlongStrictlyGreaterThanLong( [StrictlyGreaterThan( _longLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyGreaterThanLong( [StrictlyGreaterThan( _longLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyGreaterThanLong( [StrictlyGreaterThan( _longLimit )] decimal? a ) { }

    private static void MethodWithLongStrictlyLessThanLong( [StrictlyLessThan( _longLimit )] long? a ) { }

    private static void MethodWithUlongStrictlyLessThanLong( [StrictlyLessThan( _longLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyLessThanLong( [StrictlyLessThan( _longLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyLessThanLong( [StrictlyLessThan( _longLimit )] decimal? a ) { }

    #endregion Long

    #region Ulong

    // Cannot use ulongLimit by design. Covered by build test.
    private static void MethodWithLongStrictlyGreaterThanUlong( [StrictlyGreaterThan( _longLimit )] long? a ) { }

    private static void MethodWithUlongStrictlyGreaterThanUlong( [StrictlyGreaterThan( _ulongLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyGreaterThanUlong( [StrictlyGreaterThan( _ulongLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyGreaterThanUlong( [StrictlyGreaterThan( _ulongLimit )] decimal? a ) { }

    private static void MethodWithLongStrictlyLessThanUlong( [StrictlyLessThan( _ulongLimit )] long? a ) { }

    private static void MethodWithUlongStrictlyLessThanUlong( [StrictlyLessThan( _ulongLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyLessThanUlong( [StrictlyLessThan( _ulongLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyLessThanUlong( [StrictlyLessThan( _ulongLimit )] decimal? a ) { }

    #endregion Ulong

    #region Double

    // Cannot use doubleLimit by design. Covered by build test.
    private static void MethodWithLongStrictlyGreaterThanDouble( [StrictlyGreaterThan( (double) _longLimit )] long? a ) { }

    // Cannot use doubleLimit by design. Covered by build test.
    private static void MethodWithUlongStrictlyGreaterThanDouble( [StrictlyGreaterThan( (double) _ulongLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyGreaterThanDouble( [StrictlyGreaterThan( _doubleLimit )] double? a ) { }

    // Cannot use doubleLimit by design. Covered by build test.
    private static void MethodWithDecimalStrictlyGreaterThanDouble( [StrictlyGreaterThan( (double) _decimalLimit )] decimal? a ) { }

    private static void MethodWithLongStrictlyLessThanDouble( [StrictlyLessThan( _doubleLimit )] long? a ) { }

    private static void MethodWithUlongStrictlyLessThanDouble( [StrictlyLessThan( _doubleLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyLessThanDouble( [StrictlyLessThan( _doubleLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyLessThanDouble( [StrictlyLessThan( _doubleLimit )] decimal? a ) { }

    #endregion Double
}