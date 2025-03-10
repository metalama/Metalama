// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Introductions.ReturnsInt_IntParameter_SimpleBody
{
    // <target>
    class Target
    {
        [PseudoIntroduction("TestAspect")]
        public int Foo(int x)
        {
            return 42;
        }

        [PseudoIntroduction("TestAspect")]
        public static int Bar(int x)
        {
            return 42;
        }
    }
}
