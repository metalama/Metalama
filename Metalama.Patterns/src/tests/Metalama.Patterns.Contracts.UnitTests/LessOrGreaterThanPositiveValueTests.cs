// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class LessOrGreaterThanPositiveValueTests : RangeContractTestsBase
{
    private const long _longLimit = 100;
    private const ulong _ulongLimit = 100;
    private const double _doubleLimit = 100;

    private static readonly double _doubleStep = Utilities.FloatingPointHelper.GetDoubleStep( _doubleLimit );
    private static readonly decimal _decimalStep = Utilities.FloatingPointHelper.GetDecimalStep( (decimal) _doubleLimit );

    [Fact]
    public void TestMethodsWithGreaterThanAspect_Success()
    {
        TestMethodsWithGreaterThanAspect( _longLimit, _ulongLimit, _doubleLimit, (decimal) _doubleLimit );
        TestMethodsWithGreaterThanAspect( _longLimit * 2, _ulongLimit * 2, _doubleLimit * 2, (decimal) _doubleLimit * 2 );
        TestMethodsWithGreaterThanAspect( long.MaxValue, ulong.MaxValue, double.MaxValue, decimal.MaxValue );
    }

    [Fact]
    public void TestMethodsWithGreaterThanAspect_Failure()
    {
        AssertFails(
            TestMethodsWithGreaterThanAspect,
            _longLimit - 1,
            _ulongLimit - 1,
            _doubleLimit - _doubleStep,
            (decimal) _doubleLimit - _decimalStep );

        AssertFails(
            TestMethodsWithGreaterThanAspect,
            _longLimit / 2,
            _ulongLimit / 2,
            _doubleLimit / 2,
            (decimal) _doubleLimit / 2 );

        AssertFails( TestMethodsWithGreaterThanAspect, 0, 0, 0, 0 );

        AssertFails(
            TestMethodsWithGreaterThanAspect,
            _longLimit * -2,
            null,
            _doubleLimit * -2,
            (decimal) _doubleLimit * -2 );

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
        TestMethodsWithLessThanAspect( _longLimit, _ulongLimit, _doubleLimit, (decimal) _doubleLimit );
        TestMethodsWithLessThanAspect( _longLimit / 2, _ulongLimit / 2, _doubleLimit / 2, (decimal) _doubleLimit / 2 );
        TestMethodsWithLessThanAspect( 0, 0, 0, 0 );
        TestMethodsWithLessThanAspect( _longLimit * -2, null, _doubleLimit * -2, (decimal) _doubleLimit * -2 );
        TestMethodsWithLessThanAspect( long.MinValue, ulong.MinValue, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithLessThanAspect_Failure()
    {
        AssertFails(
            TestMethodsWithLessThanAspect,
            _longLimit + 1,
            _ulongLimit + 1,
            _doubleLimit + _doubleStep,
            (decimal) _doubleLimit + _decimalStep );

        AssertFails(
            TestMethodsWithLessThanAspect,
            _longLimit * 2,
            _ulongLimit * 2,
            _doubleLimit * 2,
            (decimal) _doubleLimit * 2 );

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