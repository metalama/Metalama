// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER

#pragma warning disable CS0618 // Obsolete

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.ExtensionBlocks.IntroduceOperatorIntoExtensionBlock;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Use a different receiver name to avoid collision with operator parameter
        var extensionBlock = builder.IntroduceExtensionBlock( typeof(int), "self" );

        // Introduce unary operator into extension block
        extensionBlock.IntroduceUnaryOperator(
            nameof(NegateTemplate),
            TypeFactory.GetType( typeof(int) ),
            TypeFactory.GetType( typeof(int) ),
            OperatorKind.UnaryNegation );
    }

    [Template]
    public static int NegateTemplate( int value )
    {
        return value * -1;
    }
}

// <target>
[IntroductionAttribute]
public static class TargetType { }
#endif
