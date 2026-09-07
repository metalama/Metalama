// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_10_0_OR_GREATER)
// @LanguageVersion(preview)
// @AllowPreviewLanguageFeatures
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.ExtensionBlocks.IntroduceIndexerIntoExtensionBlock;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var extensionBlock = builder.IntroduceExtensionBlock( typeof(string), "self" );

        // Introduce an indexer into the introduced extension block. C# 15 admits an indexer in an extension block
        // whose receiver parameter is named, which is the block introduced above.
        extensionBlock.IntroduceIndexer(
            typeof(int),
            nameof(GetTemplate),
            nameof(SetTemplate) );
    }

    [Template]
    public char GetTemplate( int index )
    {
        return 'x';
    }

    [Template]
    public void SetTemplate( int index, char value ) { }
}

// <target>
[IntroductionAttribute]
public static class TargetType { }
