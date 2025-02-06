// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

namespace Metalama.Patterns.Contracts.UnitTests;

#pragma warning disable LAMA5006 // Intentionally with redundant checks.

public sealed class StrictlyNegativeOrPositiveAttributeTests : RangeContractTestsBase
{
    [Fact]
    public void TestMethodsWithStrictlyPositiveAspect_Success()
    {
        TestMethodsWithStrictlyPositiveAspect( 1, 1, double.Epsilon, DecimalTolerance );
        TestMethodsWithStrictlyPositiveAspect( 100, 100, 100, 100 );
        TestMethodsWithStrictlyPositiveAspect( long.MaxValue, ulong.MaxValue, double.MaxValue, decimal.MaxValue );
    }

    [Fact]
    public void TestMethodsWithStrictlyPositiveAspect_Failure()
    {
        AssertFails( TestMethodsWithStrictlyPositiveAspect, 0, 0, 0, 0 );
        AssertFails( TestMethodsWithStrictlyPositiveAspect, -100, 0, -100, -100 );

        AssertFails(
            TestMethodsWithStrictlyPositiveAspect,
            long.MinValue,
            ulong.MinValue,
            double.MinValue,
            decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithStrictlyNegativeAspect_Success()
    {
        TestMethodsWithStrictlyNegativeAspect( -1, null, -double.Epsilon, -DecimalTolerance );
        TestMethodsWithStrictlyNegativeAspect( -100, null, -100, -100 );
        TestMethodsWithStrictlyNegativeAspect( long.MinValue, null, double.MinValue, decimal.MinValue );
    }

    [Fact]
    public void TestMethodsWithStrictlyNegativeAspect_Failure()
    {
        AssertFails( TestMethodsWithStrictlyNegativeAspect, 0, 0, 0, 0 );
        AssertFails( TestMethodsWithStrictlyNegativeAspect, 100, 0, 100, 100 );

        AssertFails(
            TestMethodsWithStrictlyNegativeAspect,
            long.MaxValue,
            ulong.MaxValue,
            double.MaxValue,
            decimal.MaxValue );
    }

    private static void TestMethodsWithStrictlyPositiveAspect(
        long? longValue,
        ulong? ulongValue,
        double? doubleValue,
        decimal? decimalValue )
    {
        MethodWithStrictlyPositiveLong( longValue );
        MethodWithStrictlyPositiveUlong( ulongValue );
        MethodWithStrictlyPositiveDouble( doubleValue );
        MethodWithStrictlyPositiveDecimal( decimalValue );
    }

    private static void TestMethodsWithStrictlyNegativeAspect(
        long? longValue,
        ulong? ulongValue,
        double? doubleValue,
        decimal? decimalValue )
    {
        MethodWithStrictlyNegativeLong( longValue );
        MethodWithStrictlyNegativeDouble( doubleValue );
        MethodWithStrictlyNegativeDecimal( decimalValue );
    }

    private static void MethodWithStrictlyPositiveLong( [StrictlyPositive] long? a ) { }

    private static void MethodWithStrictlyPositiveUlong( [StrictlyPositive] ulong? a ) { }

    private static void MethodWithStrictlyPositiveDouble( [StrictlyPositive] double? a ) { }

    private static void MethodWithStrictlyPositiveDecimal( [StrictlyPositive] decimal? a ) { }

    private static void MethodWithStrictlyNegativeLong( [StrictlyNegative] long? a ) { }

    private static void MethodWithStrictlyNegativeDouble( [StrictlyNegative] double? a ) { }

    private static void MethodWithStrictlyNegativeDecimal( [StrictlyNegative] decimal? a ) { }
}