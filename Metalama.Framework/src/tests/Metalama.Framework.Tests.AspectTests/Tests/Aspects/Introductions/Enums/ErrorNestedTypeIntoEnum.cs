// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.ErrorNestedTypeIntoEnum;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var introducedEnum = builder.IntroduceEnum( "IntroducedEnum", e => e.AddMember( "None" ) );

        // An enum declares no member at all, a nested type included, which is the rule of section 3.1 of
        // Metalama.Framework/docs/introducing-types.md.
        builder.With( introducedEnum.Declaration ).IntroduceClass( "Nested" );
    }
}

// <target>
[Introduction]
public class TargetType { }
