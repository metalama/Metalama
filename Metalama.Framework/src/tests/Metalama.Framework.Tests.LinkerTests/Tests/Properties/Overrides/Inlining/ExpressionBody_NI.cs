// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Overrides.Inlining.ExpressionBody_NI
{
    // <target>
    class Target
    {
        [PseudoNotInlineable]
        int Foo => 42;

        [PseudoOverride(nameof(Foo),"TestAspect1")]
        int Foo_Override1
        {
            get
            {
                Console.WriteLine("Before1");
                var x = link(_this.Foo.get);
                Console.WriteLine("After1");
                return x;
            }
        }
    }
}
