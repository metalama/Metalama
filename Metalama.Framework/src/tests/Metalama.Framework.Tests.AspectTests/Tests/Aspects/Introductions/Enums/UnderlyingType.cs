// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.UnderlyingType;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceEnum(
            "ByteEnum",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.UnderlyingType = SpecialType.Byte;
                e.AddMember( "Small", (byte) 1 );
                e.AddMember( "Large", (byte) 255 );
            } );

        builder.IntroduceEnum(
            "LongEnum",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.UnderlyingType = SpecialType.Int64;
                e.AddMember( "Max", long.MaxValue );
                e.AddMember( "Min", long.MinValue );
            } );

        // The underlying type int is the default one and the language implies it, so no base list is emitted.
        builder.IntroduceEnum(
            "IntEnum",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.UnderlyingType = SpecialType.Int32;
                e.AddMember( "Value", 1 );
            } );
    }
}

// <target>
[Introduction]
public class TargetType { }
