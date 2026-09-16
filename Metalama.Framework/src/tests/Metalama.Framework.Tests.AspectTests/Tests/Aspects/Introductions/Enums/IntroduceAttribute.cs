// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.IntroduceAttribute;

[AttributeUsage( AttributeTargets.Enum )]
public class MarkerAttribute : Attribute { }

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var introducedEnum = builder.IntroduceEnum(
            "IntroducedEnum",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.AddMember( "None" );
            } );

        // Introducing an attribute is the one advice that an enum accepts, because it changes no member.
        builder.With( introducedEnum.Declaration ).IntroduceAttribute( AttributeConstruction.Create( typeof(MarkerAttribute) ) );
    }
}

// <target>
[Introduction]
public class TargetType { }
