// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TargetBody.SwitchVoid_EndFlow
{
    // <target>
    class Target
    {
        void Foo(int x)
        {
            switch(x)
            {
                case 1:
                    return;
                default:
                    break;
            }
        }

        [PseudoOverride( nameof(Foo), "TestAspect")]

        void Foo_Override(int x)
        {
            Console.WriteLine("Aspect");
            link(_this.Foo, inline)(x);
        }
    }
}
