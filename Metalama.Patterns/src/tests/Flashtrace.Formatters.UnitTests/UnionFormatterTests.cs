// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;
using Xunit.Abstractions;

namespace Flashtrace.Formatters.UnitTests;

/// <summary>
/// Tests of the formatter that the repository chooses for a union of C# 15. See finding UT-14c of issue #1946.
/// </summary>
/// <remarks>
/// Without that formatter a union is formatted by the default formatter, which calls <see cref="object.ToString"/>.
/// The compiler synthesizes no <c>ToString</c> for a union declaration, so the resolved method is
/// <see cref="System.ValueType.ToString"/> and every value of the union formats to the name of the union type. A cache
/// key built from that output is the same for every value, and a cached method whose parameter is a union then returns
/// the entry of one case for another.
/// </remarks>
public sealed class UnionFormatterTests : FormattersTestsBase
{
    public UnionFormatterTests( ITestOutputHelper logger )
        : base( logger ) { }

    /// <summary>
    /// Verifies that two cases of the same union format differently, which is what a cache key requires.
    /// </summary>
    [Fact]
    public void TwoCasesFormatDifferently()
    {
        var repository = CreateRepository();

        var circle = this.Format<Shape>( repository, new Circle( 1 ) );
        var square = this.Format<Shape>( repository, new Square( 1 ) );

        Assert.NotEqual( circle, square );
    }

    /// <summary>
    /// Verifies that the output holds the value of the case rather than the name of the union type.
    /// </summary>
    [Fact]
    public void OutputHoldsTheCaseValue()
    {
        var repository = CreateRepository();

        var formatted = this.Format<Shape>( repository, new Circle( 2.5 ) );

        Assert.Contains( "2.5", formatted, StringComparison.Ordinal );
        Assert.Contains( "Circle", formatted, StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that two cases whose values format to the same text are still told apart, because the output holds the
    /// type of the case as well as its value.
    /// </summary>
    [Fact]
    public void TwoCasesOfTheSameValueFormatDifferently()
    {
        var repository = CreateRepository();

        var asInt = this.Format<Number>( repository, 1 );
        var asLong = this.Format<Number>( repository, 1L );

        Assert.NotEqual( asInt, asLong );
    }

    /// <summary>
    /// Verifies that the formatter resolved by the run-time type, which is the path that the cache key builder takes
    /// for a boxed argument, is the same as the one resolved by the static type.
    /// </summary>
    [Fact]
    public void TheWeaklyTypedPathFormatsTheCaseValueToo()
    {
        var repository = CreateRepository();

        var stringBuilder = new UnsafeStringBuilder( 1024 );
        object boxed = new Shape( new Circle( 2.5 ) );
        repository.Get( boxed.GetType() ).Format( stringBuilder, boxed );

        Assert.Equal( this.Format<Shape>( repository, new Circle( 2.5 ) ), stringBuilder.ToString() );
    }

    /// <summary>
    /// Verifies that the default value of a union, whose <c>Value</c> property is <c>null</c>, formats without
    /// throwing.
    /// </summary>
    [Fact]
    public void TheDefaultValueFormatsAsNull()
    {
        var repository = CreateRepository();

        Assert.Equal( "{null}", this.Format( repository, default(Shape) ) );
    }
}

internal record Circle( double Radius );

internal record Square( double Side );

internal union Shape( Circle, Square );

internal union Number( int, long );
