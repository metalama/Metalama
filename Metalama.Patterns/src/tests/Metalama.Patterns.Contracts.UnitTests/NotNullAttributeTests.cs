// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Contracts.UnitTests.Assets;
using Xunit;

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class NotNullAttributeTests
{
    [Fact]
    public void Given_MethodWithNotNullObjectParameter_When_NotNullPassed_Then_Success()
    {
        var cut = new NotNullTestClass();

        cut.ObjectParameterMethod( new object() );
    }

    [Fact]
    public void Given_MethodWithNotNullObjectParameter_When_NullPassed_Then_ExceptionIsThrown()
    {
        var cut = new NotNullTestClass();

        var e = Assert.Throws<ArgumentNullException>( () => cut.ObjectParameterMethod( null! ) );

        Assert.Equal( "parameter", e.ParamName );
        Assert.Contains( "parameter", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_MethodWithNotNullReferenceParameter_When_NullPassed_Then_ExceptionIsThrown()
    {
        var cut = new NotNullTestClass();

        var e = Assert.Throws<ArgumentNullException>( () => cut.ClassParameterMethod( null! ) );

        Assert.Equal( "parameter", e.ParamName );
        Assert.Contains( "parameter", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_NotNullObjectProperty_When_NullAssigned_Then_ExceptionIsThrown()
    {
        var cut = new NotNullTestClass();

        var e = Assert.Throws<ArgumentNullException>( () => cut.ObjectProperty = null! );

        Assert.Equal( "value", e.ParamName );
        Assert.Contains( "ObjectProperty", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_NotNullObjectProperty_When_NotNullAssigned_Then_Success()
    {
        var cut = new NotNullTestClass();

        cut.ObjectProperty = new object();
    }

    [Fact]
    public void Given_NotNullObjectField_When_NotNullAssigned_Then_Success()
    {
        var cut = new NotNullTestClass();

        cut.ObjectField = new object();
    }

    [Fact]
    public void Given_NotNullObjectField_When_NullAssigned_Then_ExceptionIsThrown()
    {
        var cut = new NotNullTestClass();

        var e = Assert.Throws<ArgumentNullException>( () => cut.ObjectField = null! );

        Assert.Equal( "value", e.ParamName );
        Assert.Contains( "ObjectField", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_MethodWithNotNullGenericParameter_When_NotNullPassed_Then_Success()
    {
        _ = new NotNullTestClass.B<NotNullTestClass.A>( new NotNullTestClass.A() );
    }

    [Fact]
    public void Given_MethodWithNotNullGenericParameter_When_NotNullPassed_Then_ExceptionIsThrown()
    {
        // Resharper disable once ObjectCreationAsStatement
        var e = Assert.Throws<ArgumentNullException>( () => new NotNullTestClass.B<NotNullTestClass.A>( null! ) );

        Assert.Equal( "x", e.ParamName );
        Assert.Contains( "x", e.Message, StringComparison.Ordinal );
    }
}