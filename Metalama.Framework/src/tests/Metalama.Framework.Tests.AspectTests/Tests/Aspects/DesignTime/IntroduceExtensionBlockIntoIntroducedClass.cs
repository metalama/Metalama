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

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceExtensionBlockIntoIntroducedClass;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce a top-level static class
        var ns = builder.With( builder.Target.Compilation ).WithNamespace( builder.Target.ContainingNamespace.FullName );
        var introducedClass = ns.IntroduceClass( "StringExtensions", buildType: t => t.IsStatic = true );

        // Introduce an extension block into the introduced class
        var extensionBlock = introducedClass.IntroduceExtensionBlock( typeof(string), "self" );

        // Introduce a method into the extension block
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
internal partial class TargetClass { }
#endif
