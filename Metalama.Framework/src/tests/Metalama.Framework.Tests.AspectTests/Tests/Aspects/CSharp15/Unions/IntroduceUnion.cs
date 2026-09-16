// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// The union keyword is a C# 15 feature, so the transformed code of this test needs @LanguageVersion(15.0). The
// metalamaTests.json of the CSharp15 folder requires ROSLYN_5_11_0_OR_GREATER, so this test runs in the latest
// Roslyn variant only.
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.IntroduceUnion
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceUnion(
                "Result",
                u =>
                {
                    u.Accessibility = Accessibility.Public;
                    u.AddCase( typeof(int) );
                    u.AddCase( typeof(string) );
                } );

            // The constructor of each case and the Value property are synthesized by the compiler from the union
            // declaration, so they are registered in the code model and are absent from the output below. Section
            // 4.2 of Metalama.Framework/docs/introducing-types.md states the rule.
        }
    }

    // <target>
    [Introduction]
    public class TargetType { }
}
