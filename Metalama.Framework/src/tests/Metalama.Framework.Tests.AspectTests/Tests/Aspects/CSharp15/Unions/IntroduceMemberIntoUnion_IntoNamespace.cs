// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// Verifies that a top-level introduced union that receives a member is emitted with a member list. A union
// declaration is written with a semicolon until a member is injected into it, and the arm of the linker rewriter
// that serves a type injected into a namespace has to replace that semicolon by a pair of braces, in the way the
// arm that serves a nested type does.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.IntroduceMemberIntoUnion_IntoNamespace
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.With( builder.Target.ContainingNamespace )
                .IntroduceUnion(
                    "TopLevelResult",
                    u =>
                    {
                        u.Accessibility = Accessibility.Public;
                        u.AddCase( typeof(int) );
                        u.AddCase( typeof(string) );
                    } );

            builder.With( result.Declaration ).IntroduceMethod( nameof(MethodTemplate) );
        }

        [Template]
        public int MethodTemplate() => 42;
    }

    // <target>
    namespace TargetNamespace
    {
        [Introduction]
        public class TargetType { }
    }
}
