// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// Verifies that a nested introduced enum reaches the design-time generated source. A nested introduced type is
// emitted as a member of the partial wrapper of its containing type, and the generator adds the partial modifier to
// a class, a struct, an interface and a record only, so the enum correctly receives none.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.Nested_DesignTime
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceEnum(
                "GeneratedEnum",
                buildEnum: e =>
                {
                    e.Accessibility = Accessibility.Public;
                    e.AddMember( "None" );
                    e.AddMember( "First", 1 );
                } );
        }
    }

    // <target>
    [Introduction]
    public partial class TargetType { }
}
