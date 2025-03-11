// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Jump.ReturnsInt_FJ
{
    // <target>
    class Target
    {
        int Foo(int x)
        {
            Console.WriteLine( "Original Start");
            if (x == 0)
            {
                return 42;
            }
            Console.WriteLine( "Original End");
            return x;
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        int Foo_Override(int x)
        {
            Console.WriteLine( "Before");
            int result;
            result = link( _this.Foo, inline)(x);
            Console.WriteLine( "After");
            return result;
        }
    }
}
