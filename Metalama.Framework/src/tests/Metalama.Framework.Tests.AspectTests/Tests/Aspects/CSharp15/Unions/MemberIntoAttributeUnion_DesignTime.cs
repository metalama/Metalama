// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using System.Runtime.CompilerServices;

// Verifies that the partial part generated for a struct that carries UnionAttribute is a struct and not a union.
// The attribute form of a union is an ordinary struct in the source, so a partial part written with the union
// keyword would make the compiler report CS0261. Only a union written with the union keyword is partial as a union,
// which IntroduceUnion_DesignTime covers.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.MemberIntoAttributeUnion_DesignTime
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public int IntroducedMethod()
        {
            return 0;
        }
    }

    // <target>
    [Introduction]
    [Union]
    public partial struct Pet
    {
        public Pet( int cat )
        {
            this.Value = cat;
        }

        public object? Value { get; }
    }
}
