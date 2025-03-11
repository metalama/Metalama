// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Contracts.UnitTests.Assets;
using System.Collections.Immutable;
using Xunit;

namespace Metalama.Patterns.Contracts.UnitTests;

// ReSharper disable RedundantSuppressNullableWarningExpression
public sealed class NotEmptyAttributeTests
{
    [Fact]
    public void Given_StringMethodWithNotEmpty_When_CorrectValuePassed_Then_Success()
    {
        var cut = new NotEmptyTestClass();

        cut.StringMethod( "1234567890" );
    }

    [Fact]
    public void Given_StringMethodWithNotEmpty_When_IncorrectValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new NotEmptyTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.StringMethod( string.Empty ) );

        Assert.Contains( "parameter", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_StringFieldWithNotEmpty_When_CorrectValuePassed_Then_Success()
    {
        var cut = new NotEmptyTestClass();

        cut.StringField = "1234567890";
    }

    [Fact]
    public void Given_StringFieldWithNotEmpty_When_IncorrectValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new NotEmptyTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.StringField = string.Empty );

        Assert.Contains( "StringField", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_ListMethodWithNotEmpty_When_CorrectValuePassed_Then_Success()
    {
        var cut = new NotEmptyTestClass();

        cut.ListMethod( [1, 2, 3] );
    }

    [Fact]
    public void Given_ListMethodWithNotEmpty_When_IncorrectValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new NotEmptyTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.ListMethod( [] ) );

        Assert.Contains( "parameter", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_ListFieldWithNotEmpty_When_CorrectValuePassed_Then_Success()
    {
        var cut = new NotEmptyTestClass();

        cut.ListField = [1, 2, 3];
    }

    [Fact]
    public void Given_ListFieldWithNotEmpty_When_IncorrectValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new NotEmptyTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.ListField = [] );

        Assert.Contains( "ListField", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_ICollectionPropertyWithNotEmpty_When_IncorrectValuePassed_Then_ExceptionIsThrown()
    {
        var cut = new NotEmptyTestClass();

        var e = Assert.Throws<ArgumentException>( () => cut.GenericCollectionProperty = new List<int>() );

        Assert.Contains( "GenericCollectionProperty", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_StringMethodWithNotEmptyRef_When_IncorrectValuePassed_Then_ExceptionThrown()
    {
        var cut = new NotEmptyTestClass();

        var p = "";
        var e = Assert.Throws<ArgumentException>( () => cut.StringMethodWithRef( "abc", ref p ) );

        Assert.Contains( "parameter", e.Message, StringComparison.Ordinal );
    }

    [SkippableFact( Skip = "#33302" )]
    public void Given_StringMethodWithNotEmptyRef_When_IncorrectValueReturned_Then_ExceptionThrown()
    {
        var cut = new NotEmptyTestClass();

        var p = "abc";
        var e = Assert.Throws<PostconditionViolationException>( () => cut.StringMethodWithRef( "", ref p ) );

        Assert.Contains( "parameter", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_StringMethodWithNotEmptyOut_When_IncorrectValueReturned_Then_ExceptionThrown()
    {
        var cut = new NotEmptyTestClass();

        var e = Assert.Throws<PostconditionViolationException>( () => cut.StringMethodWithOut( "", out _ ) );

        Assert.Contains( "parameter", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_StringMethodWithNotEmptyRetVal_When_IncorrectValueReturned_Then_ExceptionThrown()
    {
        var cut = new NotEmptyTestClass();

        var e = Assert.Throws<PostconditionViolationException>( () => cut.StringMethodWithRetVal( "" ) );

        Assert.Contains( "return value", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_IReadOnlyCollectionMethodWithNotEmpty_When_IncorrectValuePassed_Then_ExceptionThrown()
    {
        var cut = new NotEmptyTestClass();

        var e = Assert.Throws<ArgumentException>(
            () => cut.IReadOnlyCollectionMethod( new List<int>().AsReadOnly() ) );

        Assert.Contains( "parameter", e.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void Given_ArrayWithNotEmptyAndNotNull_When_NullPassed_Then_ArgumentNullException()
    {
        var cut = new NotEmptyTestClass();

        Assert.Throws<ArgumentNullException>(
            () => cut.Array( null! ) );
    }

    [Fact]
    public void Given_ArrayWithNotEmpty_When_SingletonPassed_Then_Succeeds()
    {
        var cut = new NotEmptyTestClass();

        cut.Array( [1] );
    }

    [Fact]
    public void Given_ArrayWithNotEmpty_When_EmptyPassed_Then_Succeeds()
    {
        var cut = new NotEmptyTestClass();

        Assert.Throws<ArgumentException>( () => cut.Array( [] ) );
    }

    [Fact]
    public void Given_ImmutableArrayWithNotEmpty_When_DefaultPassed_Then_Succeeds()
    {
        var cut = new NotEmptyTestClass();

        cut.ImmutableArray( default );
    }

    [Fact]
    public void Given_ImmutableArrayWithNotEmpty_When_SingletonPassed_Then_Succeeds()
    {
        var cut = new NotEmptyTestClass();

        cut.ImmutableArray( ImmutableArray.Create( 1 ) );
    }

    [Fact]
    public void Given_ImmutableArrayWithNotEmpty_When_EmptyPassed_Then_Succeeds()
    {
        var cut = new NotEmptyTestClass();

        Assert.Throws<ArgumentException>(
            () => cut.ImmutableArray( ImmutableArray<int>.Empty ) );
    }
}