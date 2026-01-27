// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceExtensionBlock;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var extensionBlock = builder.IntroduceExtensionBlock( typeof(string), "self" );

        extensionBlock.IntroduceMethod( nameof(GetDoubleLength) );
    }

    [Template]
    public int GetDoubleLength()
    {
        return 42;
    }
}

// <target>
[Introduction]
internal static partial class TargetClass { }
#endif
