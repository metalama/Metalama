// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// Verifies that an instance field introduced into a union is refused. The language forbids an instance field in a
// union declaration and reports CS9373, which is an error on generated code that the user cannot edit, so section 4
// of Metalama.Framework/docs/introducing-unions.md requires the advice to refuse it instead. A static field holds no
// state of the value and is permitted, which StaticFieldIntoUnion covers.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.ErrorFieldIntoUnion
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceUnion(
                "Result",
                u =>
                {
                    u.Accessibility = Accessibility.Public;
                    u.AddCase( typeof(int) );
                } );

            builder.With( result.Declaration ).IntroduceField( "_field", typeof(int) );
        }
    }

    // <target>
    [Introduction]
    public class TargetType { }
}
