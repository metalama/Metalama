// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Contracts.UnitTests.Assets;

public class EnumTestClass
{
    [EnumDataType( typeof(TestEnum) )]
    public string StringEnum;

    [EnumDataType( typeof(TestEnum) )]
    public int IntEnum;

    [EnumDataType( typeof(TestEnum) )]
    public object ObjectEnum;

    [EnumDataType( typeof(TestFlagsEnum) )]
    public int IntFlag;
}