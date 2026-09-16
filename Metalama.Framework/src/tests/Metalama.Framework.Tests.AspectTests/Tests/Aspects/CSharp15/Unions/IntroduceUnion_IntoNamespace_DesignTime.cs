// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// Verifies that a top-level introduced union reaches the editor as a union. An earlier revision of the design-time
// generator re-created a top-level introduced type through CreatePartialType, and Roslyn reports a union as a struct,
// so the editor would have received a partial struct against a union declaration, which is CS0261. The generator now
// takes the declaration from the introduction transformation. That is issue #1943.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.IntroduceUnion_IntoNamespace_DesignTime
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.ContainingNamespace )
                .IntroduceUnion(
                    "TopLevelResult",
                    u =>
                    {
                        u.Accessibility = Accessibility.Public;
                        u.AddCase( typeof(int) );
                        u.AddCase( typeof(string) );
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
