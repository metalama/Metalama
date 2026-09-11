// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// Verifies that a top-level introduced enum reaches the design-time generated source. A top-level introduced type
// takes a different route than a nested one: the generator re-creates the declaration itself, and it cannot make an
// enum partial, so the declaration is taken from the transformation instead.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.IntoNamespace_DesignTime
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.ContainingNamespace )
                .IntroduceEnum(
                    "TopLevelEnum",
                    buildEnum: e =>
                    {
                        e.Accessibility = Accessibility.Public;
                        e.AddMember( "None" );
                        e.AddMember( "First", 1 );
                    } );
        }
    }

    // <target>
    namespace TargetNamespace
    {
        [Introduction]
        public class TargetType { }
    }
}
