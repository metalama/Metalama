// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Inlining.ReturnsVoid_NI_I_NI
{
    // <target>
    internal class Target
    {
        [PseudoNotInlineable]
        private void Foo()
        {
            Console.WriteLine( "Original");
        }

        [PseudoOverride( nameof(Foo),"TestAspect1")]
        private void Foo_Override1()
        {
            Console.WriteLine( "Before1");
            Link( This.Foo)();
            Console.WriteLine( "After1");
        }

        [PseudoOverride( nameof(Foo),"TestAspect2")]
        [PseudoNotInlineable]
        private void Foo_Override2()
        {
            Console.WriteLine( "Before2");
            Link( This.Foo, Inline)();
            Console.WriteLine( "After2");
        }
    }
}
