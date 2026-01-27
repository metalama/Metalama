// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.ExtensionBlocks.ErrorTargetIsNestedClass;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Try to introduce an extension block into a nested static class - this should fail
        builder.With( builder.Target.Types.OfName( "NestedStatic" ).Single() )
            .IntroduceExtensionBlock( typeof(string), "self" );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType
{
    public static class NestedStatic { }
}
#endif
