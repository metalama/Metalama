// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// Verifies that a union introduced into a namespace is emitted as a top-level union declaration. A top-level
// introduced type reaches the compilation through a route of its own, which IntroduceUnion_IntoNamespace_DesignTime
// covers for the design-time pipeline.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.IntroduceUnion_IntoNamespace
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
