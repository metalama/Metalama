// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class StrictlyLessOrGreaterThanPositiveValueTests : RangeContractTestsBase
{
    private const long _longLimit = 100;
    private const ulong _ulongLimit = 100;
    private const double _doubleLimit = 100;

    private static readonly double _doubleStep = Utilities.FloatingPointHelper.GetDoubleStep( _doubleLimit );
    private static readonly decimal _decimalStep = Utilities.FloatingPointHelper.GetDecimalStep( (decimal) _doubleLimit );

    [Fact]
    public void TestMethodsWithStrictlyGreaterThanAspect_Success()
    {
        TestMethodsWithStrictlyGreaterThanAspect(
            _longLimit + 1,
            _ulongLimit + 1,
            _doubleLimit + _doubleStep,
            (decimal) _doubleLimit + _decimalStep );

        TestMethodsWithStrictlyGreaterThanAspect(
            _longLimit * 2,
            _ulongLimit * 2,
            _doubleLimit * 2,
            (decimal) _doubleLimit * 2 );

        TestMethodsWithStrictlyGreaterThanAspect( long.MaxValue, ulong.MaxValue, double.MaxValue, decimal.MaxValue );
    }

    [Fact]
    public void TestMethodsWithStrictlyGreaterThanAspect_Failure()
    {
        AssertFails(
            TestMethodsWithStrictlyGreaterThanAspect,
            _longLimit,
            _ulongLimit,
            _doubleLimit,
            (decimal) _doubleLimit );

        AssertFails(
            TestMethodsWithStrictlyGreaterThanAspect,
            _longLimit / 2,
            _ulongLimit / 2,
            _doubleLimit / 2,
            (decimal) _doubleLimit / 2 );

        AssertFails( TestMethodsWithStrictlyGreaterThanAspect, 0, 0, 0, 0 );

        AssertFails(
            TestMethodsWithStrictlyGreaterThanAspect,
            _longLimit * -2,
            null,
            _doubleLimit * -2,
            (decimal) _doubleLimit * -2 );

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
        TestMethodsWithStrictlyLessThanAspect(
            _longLimit - 1,
            _ulongLimit - 1,
            _doubleLimit - _doubleStep,
            (decimal) _doubleLimit - _decimalStep );

        TestMethodsWithStrictlyLessThanAspect( 0, 0, 0, 0 );

        TestMethodsWithStrictlyLessThanAspect(
            _longLimit * -2,
            _ulongLimit / 2,
            _doubleLimit * -2,
            (decimal) _doubleLimit * -2 );

        TestMethodsWithStrictlyLessThanAspect( long.MinValue, ulong.MinValue, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithStrictlyLessThanAspect_Failure()
    {
        AssertFails( TestMethodsWithStrictlyLessThanAspect, _longLimit, _ulongLimit, _doubleLimit, (decimal) _doubleLimit );

        AssertFails(
            TestMethodsWithStrictlyLessThanAspect,
            _longLimit * 2,
            null,
            _doubleLimit * 2,
            (decimal) _doubleLimit * 2 );

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

    private static void MethodWithLongStrictlyGreaterThanUlong( [StrictlyGreaterThan( _ulongLimit )] long? a ) { }

    private static void MethodWithUlongStrictlyGreaterThanUlong( [StrictlyGreaterThan( _ulongLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyGreaterThanUlong( [StrictlyGreaterThan( _ulongLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyGreaterThanUlong( [StrictlyGreaterThan( _ulongLimit )] decimal? a ) { }

    private static void MethodWithLongStrictlyLessThanUlong( [StrictlyLessThan( _ulongLimit )] long? a ) { }

    private static void MethodWithUlongStrictlyLessThanUlong( [StrictlyLessThan( _ulongLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyLessThanUlong( [StrictlyLessThan( _ulongLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyLessThanUlong( [StrictlyLessThan( _ulongLimit )] decimal? a ) { }

    #endregion Ulong

    #region Double

    private static void MethodWithLongStrictlyGreaterThanDouble( [StrictlyGreaterThan( _doubleLimit )] long? a ) { }

    private static void MethodWithUlongStrictlyGreaterThanDouble( [StrictlyGreaterThan( _doubleLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyGreaterThanDouble( [StrictlyGreaterThan( _doubleLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyGreaterThanDouble( [StrictlyGreaterThan( _doubleLimit )] decimal? a ) { }

    private static void MethodWithLongStrictlyLessThanDouble( [StrictlyLessThan( _doubleLimit )] long? a ) { }

    private static void MethodWithUlongStrictlyLessThanDouble( [StrictlyLessThan( _doubleLimit )] ulong? a ) { }

    private static void MethodWithDoubleStrictlyLessThanDouble( [StrictlyLessThan( _doubleLimit )] double? a ) { }

    private static void MethodWithDecimalStrictlyLessThanDouble( [StrictlyLessThan( _doubleLimit )] decimal? a ) { }

    #endregion Double
}