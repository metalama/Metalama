// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.ExtensionBlocks.IntroduceMultipleMethodsIntoExtensionBlock;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var extensionBlock = builder.IntroduceExtensionBlock( typeof(string), "self" );

        // Introduce multiple methods into the same extension block
        extensionBlock.IntroduceMethod(
            nameof(GetLength),
            buildMethod: m => m.Name = "GetLength" );

        extensionBlock.IntroduceMethod(
            nameof(IsEmpty),
            buildMethod: m => m.Name = "IsEmpty" );

        extensionBlock.IntroduceMethod(
            nameof(Reverse),
            buildMethod: m => m.Name = "Reverse" );
    }

    [Template]
    public int GetLength()
    {
        return 42;
    }

    [Template]
    public bool IsEmpty()
    {
        return false;
    }

    [Template]
    public string Reverse()
    {
        return "reversed";
    }
}

// <target>
[IntroductionAttribute]
public static class TargetType { }
#endif
