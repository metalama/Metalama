// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Jump.ReturnsVoid_NJ_FJ
{
    // <target>
    class Target
    {
        void Foo(int x)
        {
            Console.WriteLine( "Original Start");
            if (x == 0)
            {
                return;
            }
            Console.WriteLine( "Original End");
        }

        [PseudoOverride( nameof(Foo),"TestAspect1")]
        void Foo_Override1(int x)
        {
            Console.WriteLine( "Before1");
            link( _this.Foo, inline)(x);
            Console.WriteLine( "After1");
        }

        [PseudoOverride( nameof(Foo),"TestAspect2")]
        void Foo_Override2(int x)
        {
            Console.WriteLine( "Before2");
            link( _this.Foo, inline)(x);
            Console.WriteLine( "After2");
        }
    }
}
