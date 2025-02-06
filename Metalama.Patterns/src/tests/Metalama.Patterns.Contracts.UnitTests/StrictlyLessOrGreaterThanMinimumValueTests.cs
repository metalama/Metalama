// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

// This is a modified copy of StrictlyLessOrGreaterThanPositiveValueTests.cs
// Keep the testing logic equal for all the copies!

#pragma warning disable IDE0004  // Remove Unnecessary Cast: in this problem domain, explicit casts add clarity.
#pragma warning disable LAMA5006 // Intentionally with redundant checks.

// Resharper disable RedundantCast

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class StrictlyLessOrGreaterThanMinimumValueTests : RangeContractTestsBase
{
    private const long _longLimit = long.MinValue + 1;
    private const long _ulongLimit = 100;
    private const double _longAsDoubleLimit = 100;

    private const double _doubleLimit = double.MinValue / (1 + DoubleTolerance);

    // This has to be double because decimal is not allowed as attribute constructor value.
    // Loss of precision is a consequence.
    private const double _decimalLimit = (double) decimal.MinValue / (1 + DoubleTolerance);

    private const double _doubleStep = _doubleLimit - double.MinValue;
    private const decimal _decimalStep = (decimal) _decimalLimit - decimal.MinValue;

    [Fact]
    public void TestMethodsWithStrictlyGreaterThanAspect_Success()
    {
        MethodWithLongStrictlyGreaterThanLong( _longLimit + 1 );
        MethodWithDoubleStrictlyGreaterThanLong( (double) _longLimit + _doubleStep );
        MethodWithDecimalStrictlyGreaterThanLong( (decimal) _longLimit + _decimalStep );

        MethodWithLongStrictlyGreaterThanDouble( long.MinValue );
        MethodWithDoubleStrictlyGreaterThanDouble( _doubleLimit + _doubleStep );
        MethodWithDecimalStrictlyGreaterThanDouble( (decimal) _decimalLimit + _decimalStep );

        TestMethodsWithStrictlyGreaterThanAspect( 0, 0, 0, 0 );
        TestMethodsWithStrictlyGreaterThanAspect( long.MaxValue, ulong.MaxValue, double.MaxValue, decimal.MaxValue );
    }

    [Fact]
    public void TestMethodsWithStrictlyGreaterThanAspect_Failure()
    {
        AssertFails( TestMethodsWithStrictlyGreaterThanAspect, _longLimit, null, _doubleLimit, (decimal) _decimalLimit );
        AssertFails( TestMethodsWithStrictlyGreaterThanAspect, long.MinValue, null, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithStrictlyLessThanAspect_Success()
        => TestMethodsWithStrictlyLessThanAspect( long.MinValue, ulong.MinValue, double.MinValue, decimal.MinValue );

    [Fact]
    public void TestMethodsWithStrictlyLessThanAspect_Failure()
    {
        AssertFails( TestMethodsWithStrictlyLessThanAspect, _longLimit, null, _doubleLimit, (decimal) _decimalLimit );

        AssertFails(
            TestMethodsWithStrictlyLessThanAspect,
            _longLimit / 2,
            null,
            _doubleLimit / 2,
            (decimal) _decimalLimit / 2 );

        AssertFails( TestMethodsWithStrictlyLessThanAspect, 0, 0, 0, 0 );

        AssertFails(
            TestMethodsWithStrictlyLessThanAspect,
            _longLimit / -2,
            null,
            _doubleLimit / -2,
            (decimal) _decimalLimit / -2 );

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

    private static void MethodWithUlongStrictlyLessThanLong( [StrictlyLessThan( _ulongLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyLessThanLong( [StrictlyLessThan( _longLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyLessThanLong( [StrictlyLessThan( _longLimit )] decimal? a ) { }

    #endregion Long

    #region Double

    private static void MethodWithLongStrictlyGreaterThanDouble( [StrictlyGreaterThan( _doubleLimit )] long? a ) { }

    private static void MethodWithUlongStrictlyGreaterThanDouble( [StrictlyGreaterThan( _doubleLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyGreaterThanDouble( [StrictlyGreaterThan( _doubleLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyGreaterThanDouble( [StrictlyGreaterThan( _doubleLimit )] decimal? a ) { }

    // Cannot use doubleLimit by design. Covered by build test.
    private static void MethodWithLongStrictlyLessThanDouble( [StrictlyLessThan( _longAsDoubleLimit )] long? a ) { }

    private static void MethodWithDoubleStrictlyLessThanDouble( [StrictlyLessThan( _doubleLimit )] double? a ) { }

    // Cannot use doubleLimit by design. Covered by build test.
    private static void MethodWithDecimalStrictlyLessThanDouble( [StrictlyLessThan( (double) _decimalLimit )] decimal? a ) { }

    #endregion Double
}