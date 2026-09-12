// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.Simple;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceEnum( "DefaultEnum", e => e.AddMember( "None" ) );

        builder.IntroduceEnum(
            "PublicEnum",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.AddMember( "First" );
                e.AddMember( "Second" );
                e.AddMember( "Third" );
            } );

        // A single-member enum is the smallest one an aspect can introduce, because the callback that adds the
        // members is required.
        builder.IntroduceEnum(
            "SingleMemberEnum",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.AddMember( "Only" );
            } );
    }
}

// <target>
[Introduction]
public class TargetType { }
