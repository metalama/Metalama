// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.Flags;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Neither the language nor Metalama derives a power of two from the position of a member, so every value is
        // assigned explicitly.
        builder.IntroduceEnum(
            "Permissions",
            buildEnum: e =>
            {
                e.Accessibility = Accessibility.Public;
                e.IsFlags = true;
                e.AddMember( "None", 0 );
                e.AddMember( "Read", 1 );
                e.AddMember( "Write", 2 );
                e.AddMember( "ReadWrite", 3 );
            } );
    }
}

// <target>
[Introduction]
public class TargetType { }
