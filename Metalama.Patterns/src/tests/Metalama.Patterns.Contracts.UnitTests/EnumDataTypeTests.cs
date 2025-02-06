// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Contracts.UnitTests.Assets;
using Xunit;

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class EnumDataTypeTests
{
    [Fact]
    public void Given_FieldWithEnumDataType_When_CorrectEnumObjectPassed_Then_NoExceptionIsThrown()
    {
        var cut = new EnumTestClass();
        cut.ObjectEnum = TestEnum.Foo;
    }

    [Fact]
    public void Given_FieldWithEnumDataType_When_CorrectEnumStringPassed_Then_NoExceptionIsThrown()
    {
        var cut = new EnumTestClass();
        cut.StringEnum = TestEnum.Foo.ToString();
    }

    [Fact]
    public void Given_FieldWithEnumDataType_When_CorrectEnumIntPassed_Then_NoExceptionIsThrown()
    {
        var cut = new EnumTestClass();
        cut.IntEnum = (int) TestEnum.Foo;
    }

    [Fact]
    public void Given_FieldWithEnumDataType_When_IncorrectIntPassed_Then_ExceptionIsThrown()
    {
        var cut = new EnumTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.IntEnum = 10 );

        Assert.Contains( "IntEnum", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_FieldWithEnumDataType_When_IncorrectStringPassed_Then_ExceptionIsThrown()
    {
        var cut = new EnumTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.StringEnum = "asd" );

        Assert.Contains( "StringEnum", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_FieldWithEnumDataType_When_IncorrectObjectPassed_Then_ExceptionIsThrown()
    {
        var cut = new EnumTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.ObjectEnum = new object() );

        Assert.Contains( "ObjectEnum", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_FieldWithFlagsEnumDataType_When_CorrectEnumIntPassed_Then_NoExceptionIsThrown()
    {
        var cut = new EnumTestClass();
        cut.IntFlag = (int) TestFlagsEnum.Foo;
    }

    [Fact]
    public void Given_FieldWithFlagsEnumDataType_When_IncorrectIntPassed_Then_ExceptionIsThrown()
    {
        var cut = new EnumTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.IntFlag = 10 );

        Assert.Contains( "IntFlag", e.Message, StringComparison.Ordinal );
    }
}