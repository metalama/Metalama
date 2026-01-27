// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.ExtensionBlocks.MultipleBlocksSameType;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce two extension blocks for the same receiver type - both should be allowed
        builder.IntroduceExtensionBlock( typeof(string), "self1" );
        builder.IntroduceExtensionBlock( typeof(string), "self2" );
    }
}

// <target>
[IntroductionAttribute]
public static class TargetType { }
#endif
