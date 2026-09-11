// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.Values;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // A member whose value is given and a member whose value the language assigns may be mixed, exactly as in a
        // declaration written by hand.
        builder.IntroduceEnum(
            "ExplicitValues",
            buildEnum: e =>
            {
                e.Accessibility = Accessibility.Public;
                e.AddMember( "Zero", 0 );
                e.AddMember( "Ten", 10 );
                e.AddMember( "Eleven" );
                e.AddMember( "MinusOne", -1 );
            } );
    }
}

// <target>
[Introduction]
public class TargetType { }
