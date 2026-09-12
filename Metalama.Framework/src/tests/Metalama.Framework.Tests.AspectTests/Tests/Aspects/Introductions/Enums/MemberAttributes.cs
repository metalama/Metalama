// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.MemberAttributes;

[AttributeUsage( AttributeTargets.Field )]
public class MarkerAttribute : Attribute
{
    public MarkerAttribute( string name )
    {
        this.Name = name;
    }

    public string Name { get; }
}

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // The value that AddMember returns exists for the custom attributes and for nothing else, which is why
        // IEnumMemberBuilder declares no member of its own.
        builder.IntroduceEnum(
            "MarkedEnum",
            e =>
            {
                e.Accessibility = Accessibility.Public;

                var first = e.AddMember( "First" );
                first.AddAttribute( AttributeConstruction.Create( typeof(MarkerAttribute), new object[] { "first" } ) );

                e.AddMember( "Second" );
            } );
    }
}

// <target>
[Introduction]
public class TargetType { }
