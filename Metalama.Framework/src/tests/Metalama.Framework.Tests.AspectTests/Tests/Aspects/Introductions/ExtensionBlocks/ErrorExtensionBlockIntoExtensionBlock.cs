// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.ExtensionBlocks.ErrorExtensionBlockIntoExtensionBlock;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // First introduce an extension block
        var extensionBlock = builder.IntroduceExtensionBlock( typeof(string), "self" );

        // Try to introduce another extension block into the first extension block - this should fail
        extensionBlock.IntroduceExtensionBlock( typeof(int), "value" );
    }
}

// <target>
[IntroductionAttribute]
public static class TargetType { }
#endif
