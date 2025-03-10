// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization;

public sealed class TypedConstantSerializationTests : SerializationTestsBase
{
    private static void RoundtripTest( SerializationTestContext testContext, TypedConstant typedConstant )
    {
        var roundtrip = SerializeDeserialize( typedConstant.ToRef(), testContext ).ToTypedConstant( testContext.Compilation );

        Assert.Equal( typedConstant, roundtrip );
    }

    [Theory]
    [InlineData( 1 )]
    [InlineData( (uint) 2 )]
    [InlineData( (long) 3 )]
    [InlineData( (ulong) 4 )]
    [InlineData( (byte) 7 )]
    [InlineData( (sbyte) 8 )]
    [InlineData( (short) 9 )]
    [InlineData( (ushort) 10 )]
    [InlineData( 5.0 )]
    [InlineData( (float) 11.0 )]
    [InlineData( "str" )]
    public void TestPrimitive( object value )
    {
        using var testContext = this.CreateTestContext();

        var typedConstant = TypedConstant.Create( value );
        RoundtripTest( testContext, typedConstant );
    }

    [Theory]
    [InlineData( OperatorKind.Addition )]
    public void TestEnum( object value )
    {
        using var testContext = this.CreateTestContext();

        var typedConstant = TypedConstant.Create( value );
        RoundtripTest( testContext, typedConstant );
    }

    [Theory]
    [InlineData( new[] { 1, 2, 3 } )]
    [InlineData( new long[] { 1, 2, 3 } )]
    public void TestArray( object value )
    {
        using var testContext = this.CreateTestContext();

        var typedConstant = TypedConstant.Create( value );
        RoundtripTest( testContext, typedConstant );
    }

    [Fact]
    public void TestObjectArray()
    {
        using var testContext = this.CreateTestContext();

        var typedConstant = TypedConstant.Create( new object[] { 1, 2, OperatorKind.Addition } );

        RoundtripTest( testContext, typedConstant );
    }
}