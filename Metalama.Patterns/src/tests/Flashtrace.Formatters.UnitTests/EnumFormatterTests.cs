// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters.Implementations;
using Xunit;

namespace Flashtrace.Formatters.UnitTests;

public class EnumFormatterTests
{
    [Fact]
    public void FlagsEnumEqualsToStringTest()
    {
        FlagsEnum[] values = [0, FlagsEnum.A, FlagsEnum.B, FlagsEnum.A | FlagsEnum.B, (FlagsEnum) int.MaxValue];

        foreach ( var value in values )
        {
            Assert.Equal( value.ToString(), EnumFormatterCache<FlagsEnum>.GetString( value ) );
        }
    }

    [Flags]
    public enum FlagsEnum
    {
        A = 1,
        B = 2
    }

    [Fact]
    public void SimpleEnumEqualsToStringTest()
    {
        // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
        SimpleEnum[] values = [0, SimpleEnum.A, SimpleEnum.B, SimpleEnum.A | SimpleEnum.B, (SimpleEnum) ulong.MaxValue];

        foreach ( var value in values )
        {
            Assert.Equal( value.ToString(), EnumFormatterCache<SimpleEnum>.GetString( value ) );
        }
    }

    public enum SimpleEnum : ulong
    {
        A = 1,
        B = 2
    }
}