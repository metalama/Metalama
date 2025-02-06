// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Contracts.UnitTests.Assets;
using Xunit;

#pragma warning disable LAMA5006 // Intentionally with redundant checks.

namespace Metalama.Patterns.Contracts.UnitTests;

// ReSharper disable RedundantSuppressNullableWarningExpression
public sealed class RangeAttributeTests
{
    [Fact]
    public void Given_MethodWithInRangeParameter_When_CorrectValuePassed_Then_Success()
    {
        var cut = new RangeTestClass();

        cut.ZeroToTenMethod( 0 );
        cut.ZeroToTenMethod( 5 );
        cut.ZeroToTenMethod( 10 );
        cut.ZeroToTenNullableInt( 10 );
    }

    [Fact]
    public void Given_DecimalMethodWithInRangeParameter_When_CorrectValuePassed_Then_Success()
    {
        var cut = new RangeTestClass();

        cut.ZeroToTenDecimal( 0.0m );
        cut.ZeroToTenDecimal( 0.1m );
        cut.ZeroToTenDecimal( 5m );
        cut.ZeroToTenDecimal( 10.0m );
        cut.ZeroToTenNullableDecimal( 10.0m );
    }

    [Fact]
    public void Given_DecimalMethodWithLargeInRangeParameter_When_CorrectValuePassed_Then_Success()
    {
        var cut = new RangeTestClass();

        cut.LargeDecimalRange( decimal.MinValue );
        cut.LargeDecimalRange( decimal.MaxValue );
        cut.LargeDecimalRange( 0m );
    }

    [Fact]
    public void Given_DoubleMethodWithInRangeParameter_When_CorrectValuePassed_Then_Success()
    {
        var cut = new RangeTestClass();

        cut.ZeroToTenDouble( 0.0 );
        cut.ZeroToTenDouble( 0.1 );
        cut.ZeroToTenDouble( 5 );
        cut.ZeroToTenDouble( 10.0 );
    }

    [Fact]
    public void Given_FloatMethodWithInRangeParameter_When_CorrectValuePassed_Then_Success()
    {
        var cut = new RangeTestClass();

        cut.ZeroToTenFloat( 0.0f );
        cut.ZeroToTenFloat( 0.1f );
        cut.ZeroToTenFloat( 5 );
        cut.ZeroToTenFloat( 10.0f );
        cut.ZeroToTenNullableFloat( 10.0f );
    }

    [Fact]
    public void Given_FieldWithInRangeAttribute_When_CorrectValuePassed_Then_Success()
    {
        var cut = new RangeTestClass();

        cut.GreaterThanZeroField = 0;
        cut.GreaterThanZeroField = 5;

        cut.LessThanZeroField = 0;
        cut.LessThanZeroField = -5;
    }

    [Fact]
    public void Given_MethodWithInRangeParameter_When_ToSmallValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.ZeroToTenMethod( -10 ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_MethodWithInRangeParameter_When_ToLargeValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.ZeroToTenMethod( 20 ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_DoubleMethodWithInRangeParameter_When_ToLargeValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.ZeroToTenDouble( 10.1 ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_DoubleMethodWithInRangeParameter_When_ToSmallValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.ZeroToTenDouble( -10.0 ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_FloatMethodWithInRangeParameter_When_ToLargeValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.ZeroToTenFloat( 10.1f ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_FloatMethodWithInRangeParameter_When_ToSmallValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.ZeroToTenFloat( -10.0f ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_DecimalMethodWithInRangeParameter_When_ToLargeValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.ZeroToTenDecimal( 20.0m ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_DecimalMethodWithInRangeParameter_When_ToSmallValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.ZeroToTenDecimal( -10.0m ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_NullableDecimalMethodWithInRangeParameter_When_ToLargeValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.ZeroToTenNullableDecimal( 20.0m ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_NullableDecimalMethodWithInRangeParameter_When_ToSmallValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>(
            () => cut.ZeroToTenNullableDecimal( -10.0m ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_NullableIntMethodWithInRangeParameter_When_ToLargeValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.ZeroToTenNullableInt( 20 ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_NullableIntMethodWithInRangeParameter_When_ToSmallValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.ZeroToTenNullableInt( -10 ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_FieldWithInRangeAttribute_When_ToSmallValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.GreaterThanZeroField = -10 );

        Assert.Contains( "GreaterThanZeroField", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_FieldWithInRangeAttribute_When_ToLargeValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.LessThanZeroField = 20 );

        Assert.Contains( "LessThanZeroField", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_MethodWithInRangeRef_When_IncorrectValuePassed_Then_ExceptionThrown()
    {
        var cut = new RangeTestClass();

        long? p = -1;
        var e = Assert.Throws<ArgumentOutOfRangeException>( () => cut.ZeroToTenNullableIntRef( 1, ref p ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [SkippableFact( Skip = "#33302" )]
    public void Given_MethodWithInRangeRef_When_IncorrectValueReturned_Then_ExceptionThrown()
    {
        var cut = new RangeTestClass();

        long? p = 1;

        var e = Assert.Throws<PostconditionViolationException>(
            () => cut.ZeroToTenNullableIntRef( -1, ref p ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_MethodWithInRangeOut_When_IncorrectValueReturned_Then_ExceptionThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<PostconditionViolationException>(
            () =>
                cut.ZeroToTenNullableIntOut( -1, out _ ) );

        Assert.Contains( "parameter", e!.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_MethodWithInRangeRetVal_When_IncorrectValueReturned_Then_ExceptionThrown()
    {
        var cut = new RangeTestClass();

        var e = Assert.Throws<PostconditionViolationException>( () => cut.ZeroToTenNullableIntRetVal( -1 ) );

        Assert.Contains( "return value", e!.Message, StringComparison.Ordinal );
    }
}