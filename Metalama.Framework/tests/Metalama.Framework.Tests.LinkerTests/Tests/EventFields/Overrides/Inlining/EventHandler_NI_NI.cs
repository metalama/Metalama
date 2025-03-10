// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.LinkerTests.Tests.EventFields.Overrides.Inlining.EventHandler_NI_NI
{
    // <target>
    class Target
    {
        event EventHandler? Foo;

        [PseudoOverride( nameof(Foo),"TestAspect")]
        [PseudoNotInlineable]
        event EventHandler? Foo_Override
        {
            add
            {
                Console.WriteLine("Before");
                link[_this.Foo.add] += value;
                Console.WriteLine("After");
            }

            remove
            {
                Console.WriteLine("Before");
                link[_this.Foo.remove] -= value;
                Console.WriteLine("After");
            }
        }
    }
}
