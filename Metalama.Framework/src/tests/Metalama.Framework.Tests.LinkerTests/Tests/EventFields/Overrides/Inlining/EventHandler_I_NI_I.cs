// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.LinkerTests.Tests.EventFields.Overrides.Inlining.EventHandler_I_NI_I
{
    // <target>
    class Target
    {
        event EventHandler? Foo;

        [PseudoOverride(nameof(Foo), "TestAspect1")]
        event EventHandler? Foo_Override1
        {
            add
            {
                Console.WriteLine("Before1");
                link[_this.Foo.add, inline] += value;
                Console.WriteLine("After1");
            }

            remove
            {
                Console.WriteLine("Before1");
                link[_this.Foo.remove, inline] -= value;
                Console.WriteLine("After1");
            }
        }

        [PseudoOverride(nameof(Foo), "TestAspect2")]
        event EventHandler? Foo_Override2
        {
            add
            {
                Console.WriteLine( "Before2");
                link[_this.Foo.add] += value;
                Console.WriteLine( "After2");
            }

            remove
            {
                Console.WriteLine("Before2");
                link[_this.Foo.remove] -= value;
                Console.WriteLine("After2");
            }
        }
    }
}
