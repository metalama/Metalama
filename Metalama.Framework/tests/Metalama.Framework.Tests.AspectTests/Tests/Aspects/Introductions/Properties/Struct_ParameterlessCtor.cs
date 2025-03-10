// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.Struct_ParameterlessCtor
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public int IntroducedProperty { get; set; }

        [Introduce]
        public int IntroducedProperty_Initializer { get; set; } = 42;

        [Introduce]
        public static int IntroducedProperty_Static { get; set; }

        [Introduce]
        public static int IntroducedProperty_Static_Initializer { get; set; } = 42;
    }

    // <target>
    [Introduction]
    internal struct TargetStruct
    {
        public TargetStruct() { }

        public int ExistingField = 42;

        public int ExistingProperty { get; set; } = 42;
    }
}