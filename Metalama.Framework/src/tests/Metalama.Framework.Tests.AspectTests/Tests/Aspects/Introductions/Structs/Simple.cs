// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Structs.Simple;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceStruct( "TestNestedStruct" );

        builder.IntroduceStruct( "PublicStruct", buildType: t => t.Accessibility = Accessibility.Public );

        // The parameterless constructor that the compiler synthesizes for a struct is registered in the code model
        // and is not emitted, so it does not appear in the output below. Section 4.2 of
        // Metalama.Framework/docs/future/introducing-types.md states the rule.
    }
}

// <target>
[Introduction]
public class TargetType { }
