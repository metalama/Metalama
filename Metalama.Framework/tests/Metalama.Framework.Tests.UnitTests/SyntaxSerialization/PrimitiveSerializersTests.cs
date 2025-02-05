// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System;
using Xunit;

#pragma warning disable CA1305 // Specify IFormatProvider

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class PrimitiveSerializersTests : SerializerTestsBase
    {
        [Fact]
        public void TestInt()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "42", testContext.Serialize( 42 ).ToString() );
            Assert.Equal( "2147483647", testContext.Serialize( int.MaxValue ).ToString() );
            Assert.Equal( "-2147483648", testContext.Serialize( int.MinValue ).ToString() );
        }

        [Fact]
        public void TestChar()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "'\\0'", testContext.Serialize( '\0' ).ToString() );
            Assert.Equal( "'\\n'", testContext.Serialize( '\n' ).ToString() );
            Assert.Equal( "'A'", testContext.Serialize( 'A' ).ToString() );
            Assert.Equal( "'ř'", testContext.Serialize( 'ř' ).ToString() );
            Assert.Equal( "'衛'", testContext.Serialize( '衛' ).ToString() );
        }

        [Fact]
        public void TestBool()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "true", testContext.Serialize( true ).ToString() );
            Assert.Equal( "false", testContext.Serialize( false ).ToString() );
        }

        [Fact]
        public void TestByte()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "42", testContext.Serialize<byte>( 42 ).ToString() );
            Assert.Equal( "0", testContext.Serialize<byte>( 0 ).ToString() );
            Assert.Equal( "255", testContext.Serialize<byte>( 255 ).ToString() );
        }

        [Fact]
        public void TestSByte()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "42", testContext.Serialize<sbyte>( 42 ).ToString() );
            Assert.Equal( "127", testContext.Serialize<sbyte>( 127 ).ToString() );
            Assert.Equal( "-128", testContext.Serialize<sbyte>( -128 ).ToString() );
        }

        [Fact]
        public void TestUShort()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "42", testContext.Serialize<ushort>( 42 ).ToString() );
            Assert.Equal( ushort.MaxValue.ToString(), testContext.Serialize( ushort.MaxValue ).ToString() );
            Assert.Equal( ushort.MinValue.ToString(), testContext.Serialize( ushort.MinValue ).ToString() );
        }

        [Fact]
        public void TestShort()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "42", testContext.Serialize<short>( 42 ).ToString() );
            Assert.Equal( short.MaxValue.ToString(), testContext.Serialize( short.MaxValue ).ToString() );
            Assert.Equal( short.MinValue.ToString(), testContext.Serialize( short.MinValue ).ToString() );
        }

        [Fact]
        public void TestULong()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "42UL", testContext.Serialize<ulong>( 42 ).ToString() );
            Assert.Equal( ulong.MaxValue + "UL", testContext.Serialize( ulong.MaxValue ).ToString() );
            Assert.Equal( ulong.MinValue + "UL", testContext.Serialize( ulong.MinValue ).ToString() );
        }

        [Fact]
        public void TestUInt()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "42U", testContext.Serialize<uint>( 42 ).ToString() );
            Assert.Equal( uint.MaxValue + "U", testContext.Serialize( uint.MaxValue ).ToString() );
            Assert.Equal( uint.MinValue + "U", testContext.Serialize( uint.MinValue ).ToString() );
        }

        [Fact]
        public void TestLong()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "42L", testContext.Serialize<long>( 42 ).ToString() );
            Assert.Equal( "9223372036854775807L", testContext.Serialize( long.MaxValue ).ToString() );
            Assert.Equal( "-9223372036854775808L", testContext.Serialize( long.MinValue ).ToString() );
        }

        [Fact]
        public void TestFloat()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "42F", testContext.Serialize<float>( 42 ).ToString() );
#if NET5_0_OR_GREATER // The result is slightly different in .NET Framework but there is probably no point to investigate.            
            Assert.Equal( "3.1415927F", testContext.Serialize( 3.1415927F ).ToString() );
            Assert.Equal( "-3.402823E+38F", testContext.Serialize( -3.402823E+38F ).ToString() );
            Assert.Equal( "3.402823E+38F", testContext.Serialize( 3.402823E+38F ).ToString() );
            Assert.Equal( "1E-45F", testContext.Serialize( float.Epsilon ).ToString() );
#endif
            Assert.Equal( "float.PositiveInfinity", testContext.Serialize( float.PositiveInfinity ).ToString() );
            Assert.Equal( "float.NegativeInfinity", testContext.Serialize( float.NegativeInfinity ).ToString() );
            Assert.Equal( "float.NaN", testContext.Serialize( float.NaN ).ToString() );
            Assert.Equal( "0F", testContext.Serialize<float>( 0 ).ToString() );
            Assert.Equal( "0F", testContext.Serialize<float>( -0 ).ToString() );
        }

        [Fact]
        public void TestDouble()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "42", testContext.Serialize<double>( 42 ).ToString() );
#if NET5_0_OR_GREATER // The result is slightly different in .NET Framework but there is probably no point to investigate.
            Assert.Equal( "3.14159285", testContext.Serialize( 3.14159285 ).ToString() );
            Assert.Equal( "-3.402823E+38", testContext.Serialize( -3.402823E+38 ).ToString() );
            Assert.Equal( "3.402823E+38", testContext.Serialize( 3.402823E+38 ).ToString() );
            Assert.Equal( "5E-324", testContext.Serialize( double.Epsilon ).ToString() );
#endif
            Assert.Equal( "double.PositiveInfinity", testContext.Serialize( double.PositiveInfinity ).ToString() );
            Assert.Equal( "double.NegativeInfinity", testContext.Serialize( double.NegativeInfinity ).ToString() );
            Assert.Equal( "double.NaN", testContext.Serialize( double.NaN ).ToString() );
            Assert.Equal( "0", testContext.Serialize<double>( 0 ).ToString() );
            Assert.Equal( "0", testContext.Serialize<double>( -0 ).ToString() );
        }

        [Fact]
        public void TestDecimal()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "42M", testContext.Serialize<decimal>( 42 ).ToString() );
            Assert.Equal( "1.0M", testContext.Serialize( 1.0M ).ToString() );
            Assert.Equal( "1.00M", testContext.Serialize( 1.00M ).ToString() );
            Assert.Equal( "4.2M", testContext.Serialize( 4.2M ).ToString() );
            Assert.Equal( "-340282300M", testContext.Serialize( -3.402823E+8M ).ToString() );
            Assert.Equal( "340282300M", testContext.Serialize( 3.402823E+8M ).ToString() );
            Assert.Equal( "-79228162514264337593543950335M", testContext.Serialize( decimal.MinValue ).ToString() );
            Assert.Equal( "79228162514264337593543950335M", testContext.Serialize( decimal.MaxValue ).ToString() );
            Assert.Equal( "0M", testContext.Serialize<decimal>( 0 ).ToString() );
        }

        [Fact]
        public unsafe void TestUIntPtr()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var pointer = (int*) 300;
#if NET7_0_OR_GREATER
            Assert.Equal( "new nint(-400L)", testContext.Serialize( new IntPtr( -400L ) ).NormalizeWhitespace().ToString() );
            Assert.Equal( "new nint(-200L)", testContext.Serialize( new IntPtr( -200 ) ).NormalizeWhitespace().ToString() );
            Assert.Equal( "new nint(300L)", testContext.Serialize( new IntPtr( pointer ) ).NormalizeWhitespace().ToString() );
#else
            Assert.Equal( "new global::System.UIntPtr(400UL)", testContext.Serialize( new UIntPtr( 400UL ) ).NormalizeWhitespace().ToString() );
            Assert.Equal( "new global::System.UIntPtr(200UL)", testContext.Serialize( new UIntPtr( 200 ) ).NormalizeWhitespace().ToString() );
            Assert.Equal( "new global::System.UIntPtr(300UL)", testContext.Serialize( new UIntPtr( pointer ) ).NormalizeWhitespace().ToString() );
#endif
        }

        [Fact]
        public unsafe void TestIntPtr()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var pointer = (int*) 300;
#if NET7_0_OR_GREATER
            Assert.Equal( "new nint(-400L)", testContext.Serialize( new IntPtr( -400L ) ).NormalizeWhitespace().ToString() );
            Assert.Equal( "new nint(-200L)", testContext.Serialize( new IntPtr( -200 ) ).NormalizeWhitespace().ToString() );
            Assert.Equal( "new nint(300L)", testContext.Serialize( new IntPtr( pointer ) ).NormalizeWhitespace().ToString() );
#else
            Assert.Equal( "new global::System.IntPtr(-400L)", testContext.Serialize( new IntPtr( -400L ) ).NormalizeWhitespace().ToString() );
            Assert.Equal( "new global::System.IntPtr(-200L)", testContext.Serialize( new IntPtr( -200 ) ).NormalizeWhitespace().ToString() );
            Assert.Equal( "new global::System.IntPtr(300L)", testContext.Serialize( new IntPtr( pointer ) ).NormalizeWhitespace().ToString() );
#endif
        }
    }
}