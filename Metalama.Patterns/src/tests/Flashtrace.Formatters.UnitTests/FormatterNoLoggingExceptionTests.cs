// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Formatters.UnitTests.Assets;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable UseArrayEmptyMethod
#pragma warning disable CA1825

namespace Flashtrace.Formatters.UnitTests;

public sealed class FormatterNoLoggingExceptionTests : FormattersTestsBase
{
    public FormatterNoLoggingExceptionTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public void ThrowingConstructor()
    {
        var formatters = CreateRepository( b => b.AddFormatter( typeof(IEnumerable<>), typeof(ThrowingFormatter<>) ) );

        var result = this.Format<IEnumerable<int>>( formatters, new int[0] );

        Assert.True( ThrowingFormatter<int>.Ran );
        Assert.Equal( "{int[]}", result );
    }

    [Fact]
    public void PrivateConstructor()
    {
        var formatters = CreateRepository( b => b.AddFormatter( typeof(IEnumerable<>), typeof(NoConstructorFormatter<>) ) );

        var result = this.Format<IEnumerable<int>>( formatters, new int[0] );

        Assert.Equal( "{int[]}", result );
    }

    [Fact]
    public void BadRegistration()
    {
        var formatters = CreateRepository( b => b.AddFormatter( typeof(IComparable<>), typeof(ThrowingFormatter<>) ) );

        var result = this.Format<IComparable<int>>( formatters, 0 );

        Assert.True( ThrowingFormatter<int>.Ran );
        Assert.Equal( "0", result );
    }
}