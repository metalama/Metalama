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
        builder.IntroduceEnum( "DefaultEnum", buildEnum: e => e.AddMember( "None" ) );

        builder.IntroduceEnum(
            "PublicEnum",
            buildEnum: e =>
            {
                e.Accessibility = Accessibility.Public;
                e.AddMember( "First" );
                e.AddMember( "Second" );
                e.AddMember( "Third" );
            } );

        // An enum that declares no member is a valid declaration.
        builder.IntroduceEnum( "EmptyEnum", buildEnum: e => e.Accessibility = Accessibility.Public );
    }
}

// <target>
[Introduction]
public class TargetType { }
