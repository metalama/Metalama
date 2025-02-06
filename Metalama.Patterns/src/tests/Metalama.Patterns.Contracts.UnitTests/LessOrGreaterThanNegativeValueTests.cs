// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

// This is a modified copy of LessOrGreaterThanPositiveValueTests.cs
// Keep the testing logic equal for all the copies!

#pragma warning disable LAMA5006 // Intentionally with redundant checks.

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class LessOrGreaterThanNegativeValueTests : RangeContractTestsBase
{
    private const long _longLimit = -100;
    private const double _doubleLimit = -100;
    private const double _longAsDoubleLimit = -100;

    private static readonly double _doubleStep = Utilities.FloatingPointHelper.GetDoubleStep( _doubleLimit );
    private static readonly decimal _decimalStep = Utilities.FloatingPointHelper.GetDecimalStep( (decimal) _doubleLimit );

    [Fact]
    public void TestMethodsWithGreaterThanAspect_Success()
    {
        TestMethodsWithGreaterThanAspect( _longLimit, null, _doubleLimit, (decimal) _doubleLimit );
        TestMethodsWithGreaterThanAspect( _longLimit / 2, null, _doubleLimit / 2, (decimal) _doubleLimit / 2 );
        TestMethodsWithGreaterThanAspect( 0, 0, 0, 0 );

        TestMethodsWithGreaterThanAspect(
            _longLimit * -2,
            (ulong) Math.Abs( _longLimit ) * 2,
            _doubleLimit * -2,
            (decimal) _doubleLimit * -2 );

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

        AssertFails(
            TestMethodsWithGreaterThanAspect,
            _longLimit * 2,
            null,
            _doubleLimit * 2,
            (decimal) _doubleLimit * 2 );

        AssertFails( TestMethodsWithGreaterThanAspect, long.MinValue, null, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithLessThanAspect_Success()
    {
        TestMethodsWithLessThanAspect( _longLimit, null, _doubleLimit, (decimal) _doubleLimit );
        TestMethodsWithLessThanAspect( _longLimit * 2, null, _doubleLimit * 2, (decimal) _doubleLimit * 2 );
        TestMethodsWithLessThanAspect( long.MinValue, null, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithLessThanAspect_Failure()
    {
        AssertFails(
            TestMethodsWithLessThanAspect,
            _longLimit + 1,
            null,
            _doubleLimit + _doubleStep,
            (decimal) _doubleLimit + _decimalStep );

        AssertFails( TestMethodsWithLessThanAspect, _longLimit / 2, null, _doubleLimit / 2, (decimal) _doubleLimit / 2 );
        AssertFails( TestMethodsWithLessThanAspect, 0, 0, 0, 0 );

        AssertFails(
            TestMethodsWithLessThanAspect,
            _longLimit * -2,
            (ulong) Math.Abs( _longLimit ) * 2,
            _doubleLimit * -2,
            (decimal) _doubleLimit * -2 );

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

    private static void MethodWithDecimalLessThanDouble( [LessThan( _doubleLimit )] decimal? a ) { }

    #endregion Double
}