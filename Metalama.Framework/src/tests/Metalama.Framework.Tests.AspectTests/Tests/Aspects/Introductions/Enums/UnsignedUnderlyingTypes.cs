// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.UnsignedUnderlyingTypes;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // The ulong overload is the one that AddMember(string, long) cannot serve, because a value above
        // long.MaxValue does not fit in a long. The member below is the largest value the language accepts.
        builder.IntroduceEnum(
            "UnsignedLong",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.UnderlyingType = SpecialType.UInt64;
                e.AddMember( "Zero", 0UL );
                e.AddMember( "Max", ulong.MaxValue );
            } );

        builder.IntroduceEnum(
            "UnsignedByte",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.UnderlyingType = SpecialType.Byte;
                e.AddMember( "Min", byte.MinValue );
                e.AddMember( "Max", byte.MaxValue );
            } );

        builder.IntroduceEnum(
            "Signed",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.UnderlyingType = SpecialType.SByte;
                e.AddMember( "Min", sbyte.MinValue );
                e.AddMember( "Max", sbyte.MaxValue );
            } );

        builder.IntroduceEnum(
            "UnsignedShort",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.UnderlyingType = SpecialType.UInt16;
                e.AddMember( "Max", ushort.MaxValue );
            } );

        builder.IntroduceEnum(
            "UnsignedInt",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.UnderlyingType = SpecialType.UInt32;
                e.AddMember( "Max", uint.MaxValue );
            } );
    }
}

// <target>
[Introduction]
public class TargetType { }
