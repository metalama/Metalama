// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// Verifies that a static field is introduced into a union. The language forbids an instance field in a union
// declaration, because it holds the state of the value, and permits a static field, which does not.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.StaticFieldIntoUnion
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

            builder.With( result.Declaration ).IntroduceField( "_count", typeof(int), buildField: f => f.IsStatic = true );
        }
    }

    // <target>
    [Introduction]
    public class TargetType { }
}
