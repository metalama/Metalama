// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters.Implementations;
using System.Globalization;
using Xunit;
using Xunit.Abstractions;

namespace Flashtrace.Formatters.UnitTests;

public class IntegerFormatterTests : FormattersTestsBase
{
    public IntegerFormatterTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public void ByteTest()
    {
        byte[] values = [0, 1, 13, 42, 127, 128, 255];

        var formatter = new ByteFormatter( CreateRepository() );
        var stringBuilder = new UnsafeStringBuilder( 1024 );

        foreach ( var value in values )
        {
            formatter.Format( stringBuilder, value );

            Assert.Equal( value.ToString( CultureInfo.InvariantCulture ), stringBuilder.ToString() );

            stringBuilder.Clear();
        }
    }

    [Fact]
    public void SByteTest()
    {
        sbyte[] values = [0, 1, 13, 42, 127, -1, -13, -127, -128];

        var formatter = new SByteFormatter( CreateRepository() );
        var stringBuilder = new UnsafeStringBuilder();

        foreach ( var value in values )
        {
            formatter.Format( stringBuilder, value );

            Assert.Equal( value.ToString( CultureInfo.InvariantCulture ), stringBuilder.ToString() );

            stringBuilder.Clear();
        }
    }

    [Fact]
    public void UInt16Test()
    {
        ushort[] values = [0, 1, 13, 42, 127, 128, 255, 256, 10000, 32767, 32768, 65534, 65535];

        var formatter = new UInt16Formatter( CreateRepository() );
        var stringBuilder = new UnsafeStringBuilder();

        foreach ( var value in values )
        {
            formatter.Format( stringBuilder, value );

            Assert.Equal( value.ToString( CultureInfo.InvariantCulture ), stringBuilder.ToString() );

            stringBuilder.Clear();
        }
    }

    [Fact]
    public void Int16Test()
    {
        short[] values = [0, 1, 13, 42, 127, 128, 255, 256, 10000, 32767, -1, -13, -127, -128, -10000, -32767, -32768];

        var formatter = new Int16Formatter( CreateRepository() );
        var stringBuilder = new UnsafeStringBuilder();

        foreach ( var value in values )
        {
            formatter.Format( stringBuilder, value );

            Assert.Equal( value.ToString( CultureInfo.InvariantCulture ), stringBuilder.ToString() );

            stringBuilder.Clear();
        }
    }

    [Fact]
    public void UInt32Test()
    {
        uint[] values =
        [
            0,
            1,
            13,
            42,
            127,
            128,
            255,
            256,
            10000,
            32767,
            32768,
            65534,
            65535,
            65536,
            142857,
            1000000,
            33550336,
            2147483647,
            2147483648,
            4294967294,
            4294967295
        ];

        var formatter = new UInt32Formatter( CreateRepository() );
        var stringBuilder = new UnsafeStringBuilder();

        foreach ( var value in values )
        {
            formatter.Format( stringBuilder, value );

            Assert.Equal( value.ToString( CultureInfo.InvariantCulture ), stringBuilder.ToString() );

            stringBuilder.Clear();
        }
    }

    [Fact]
    public void Int32Test()
    {
        int[] values =
        [
            0,
            1,
            13,
            42,
            127,
            128,
            255,
            256,
            10000,
            32767,
            32768,
            65534,
            65535,
            65536,
            142857,
            1000000,
            33550336,
            2147483647,
            -1,
            -13,
            -127,
            -128,
            -10000,
            -32767,
            -32768,
            -65536,
            -2147483647,
            -2147483648
        ];

        var formatter = new Int32Formatter( CreateRepository() );
        var stringBuilder = new UnsafeStringBuilder();

        foreach ( var value in values )
        {
            formatter.Format( stringBuilder, value );

            Assert.Equal( value.ToString( CultureInfo.InvariantCulture ), stringBuilder.ToString() );

            stringBuilder.Clear();
        }
    }

    /*
    [Fact]
    public void Int32AllTest()
    {
        Int32Formatter formatter = Int32Formatter.Instance;
        UnsafeStringBuilder stringBuilder = new UnsafeStringBuilder();

        for (int value = 0; value < 1000000000; value++)
        {
            formatter.Write(stringBuilder, value);

            AssertEx.Equal(value.ToString(CultureInfo.InvariantCulture), stringBuilder.ToString());

            stringBuilder.Clear();
        }
    }
    */

    [Fact]
    public void UInt64Test()
    {
        ulong[] values =
        [
            0,
            1,
            13,
            42,
            127,
            128,
            255,
            256,
            10000,
            32767,
            32768,
            65534,
            65535,
            65536,
            142857,
            1000000,
            33550336,
            2147483647,
            2147483648,
            4294967294,
            4294967295,
            8589869056,
            137438691328,
            2305843008139952128,
            9223372036854775807,
            9223372036854775808,
            18446744073709551615
        ];

        var formatter = new UInt64Formatter( CreateRepository() );
        var stringBuilder = new UnsafeStringBuilder();

        foreach ( var value in values )
        {
            formatter.Format( stringBuilder, value );

            Assert.Equal( value.ToString( CultureInfo.InvariantCulture ), stringBuilder.ToString() );

            stringBuilder.Clear();
        }
    }

    [Fact]
    public void Int64Test()
    {
        long[] values =
        [
            0,
            1,
            13,
            42,
            127,
            128,
            255,
            256,
            10000,
            32767,
            32768,
            65534,
            65535,
            65536,
            142857,
            1000000,
            33550336,
            2147483647,
            2147483648,
            4294967294,
            4294967295,
            8589869056,
            137438691328,
            2305843008139952128,
            9223372036854775807,
            -1,
            -13,
            -127,
            -128,
            -10000,
            -32767,
            -32768,
            -65536,
            -2147483647,
            -2147483648,
            -4294967294,
            -2305843008139952128,
            -9223372036854775807,
            -9223372036854775808
        ];

        var formatter = new Int64Formatter( CreateRepository() );
        var stringBuilder = new UnsafeStringBuilder();

        foreach ( var value in values )
        {
            formatter.Format( stringBuilder, value );

            Assert.Equal( value.ToString( CultureInfo.InvariantCulture ), stringBuilder.ToString() );

            stringBuilder.Clear();
        }
    }
}