// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

#pragma warning disable LAMA5006 // Intentionally with redundant checks.

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class NegativeOrPositiveAttributeTests : RangeContractTestsBase
{
    [Fact]
    public void TestMethodsWithPositiveAspect_Success()
    {
        TestMethodsWithPositiveAspect( 0, 0, 0, 0 );
        TestMethodsWithPositiveAspect( 100, 100, 100, 100 );
        TestMethodsWithPositiveAspect( long.MaxValue, ulong.MaxValue, double.MaxValue, decimal.MaxValue );
    }

    [Fact]
    public void TestMethodsWithPositiveAspect_Failure()
    {
        AssertFails( TestMethodsWithPositiveAspect, -1, null, -double.Epsilon, -DecimalTolerance );
        AssertFails( TestMethodsWithPositiveAspect, -100, null, -100, -100 );
        AssertFails( TestMethodsWithPositiveAspect, long.MinValue, null, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithNegativeAspect_Success()
    {
        TestMethodsWithNegativeAspect( 0, 0, 0, 0 );
        TestMethodsWithNegativeAspect( -100, null, -100, -100 );
        TestMethodsWithNegativeAspect( long.MinValue, null, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithNegativeAspect_Failure()
    {
        AssertFails( TestMethodsWithNegativeAspect, 1, 1, double.Epsilon, DecimalTolerance );
        AssertFails( TestMethodsWithNegativeAspect, 100, 100, 100, 100 );
        AssertFails( TestMethodsWithNegativeAspect, long.MinValue, ulong.MaxValue, double.MaxValue, decimal.MaxValue );
    }

    private static void TestMethodsWithPositiveAspect(
        long? longValue,
        ulong? ulongValue,
        double? doubleValue,
        decimal? decimalValue )
    {
        MethodWithPositiveLong( longValue );
        MethodWithPositiveUlong( ulongValue );
        MethodWithPositiveDouble( doubleValue );
        MethodWithPositiveDecimal( decimalValue );
    }

    private static void TestMethodsWithNegativeAspect(
        long? longValue,
        ulong? ulongValue,
        double? doubleValue,
        decimal? decimalValue )
    {
        MethodWithNegativeLong( longValue );
        MethodWithNegativeUlong( ulongValue );
        MethodWithNegativeDouble( doubleValue );
        MethodWithNegativeDecimal( decimalValue );
    }

    private static void MethodWithPositiveLong( [Positive] long? a ) { }

    private static void MethodWithPositiveUlong( [Positive] ulong? a ) { }

    private static void MethodWithPositiveDouble( [Positive] double? a ) { }

    private static void MethodWithPositiveDecimal( [Positive] decimal? a ) { }

    private static void MethodWithNegativeLong( [Negative] long? a ) { }

    private static void MethodWithNegativeUlong( [Negative] ulong? a ) { }

    private static void MethodWithNegativeDouble( [Negative] double? a ) { }

    private static void MethodWithNegativeDecimal( [Negative] decimal? a ) { }
}