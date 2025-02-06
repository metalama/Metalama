// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Contracts.UnitTests.Assets;
using Xunit;

namespace Metalama.Patterns.Contracts.UnitTests;

// ReSharper disable InconsistentNaming
// ReSharper disable RedundantSuppressNullableWarningExpression
public sealed class RegularExpressionAttributeTests
{
    [Fact]
    public void Given_MethodWithRegexMatch_When_CorrectValuePassed_Then_Success()
    {
        var cut = new RegexTestClass();

        cut.SetEmail( "test@postsharp.test" );
    }

    [Fact]
    public void Given_MethodWithRegexMatch_When_IncorrectValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RegexTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.SetEmail( "asd" ) );

        Assert.Contains( "email", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_FieldWithRegexMatch_When_CorrectValuePassed_Then_Success()
    {
        var cut = new RegexTestClass();

        cut.Email = "test@postsharp.test";
    }

    [Fact]
    public void Given_FieldWithRegexMatch_When_IncorrectValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RegexTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.Email = "asd" );

        Assert.Contains( "Email", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_FieldWithEmail_When_CorrectValuePassed_Then_Success()
    {
        var cut = new RegexTestClass();

        cut.Email2 = "test@postsharp.test";
    }

    [Fact]
    public void Given_FieldEmail_When_IncorrectValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RegexTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.Email2 = "asd" );

        Assert.Contains( "Email2", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_FieldWithPhone_When_CorrectValuePassed_Then_Success()
    {
        var cut = new RegexTestClass();

        cut.PhoneField = "644-14-90";
    }

    [Fact]
    public void Given_FieldPhone_When_IncorrectValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RegexTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.PhoneField = "a123" );

        Assert.Contains( "PhoneField", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_FieldWithUrl_When_CorrectValuePassed_Then_Success()
    {
        var cut = new RegexTestClass();

        cut.UrlField = "http://www.sharpcrafters.com/";
    }

    [Fact]
    public void Given_FieldUrl_When_IncorrectValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new RegexTestClass();

        // ReSharper disable once StringLiteralTypo
        var e = Assert.Throws<ArgumentException>( () => cut.UrlField = "dslkfusd" );

        Assert.Contains( "UrlField", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_FieldWithRegexMatch_When_IncorrectValuePassed_Then_ExceptionMessageIsCorrect_1()
    {
        var cut = new RegexTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.PatternEscaping1 = "hello" );

        Assert.Contains( "must match the regular expression '^[a-z]{4}$'.", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_FieldWithRegexMatch_When_IncorrectValuePassed_Then_ExceptionMessageIsCorrect_2()
    {
        var cut = new RegexTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.PatternEscaping2 = "{hello}" );

        Assert.Contains( "must match the regular expression '^\\{[a-z]{4}}$'.", e.Message, StringComparison.Ordinal );
    }
}