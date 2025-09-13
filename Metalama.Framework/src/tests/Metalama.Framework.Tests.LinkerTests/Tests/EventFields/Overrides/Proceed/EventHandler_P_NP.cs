// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.LinkerTests.Tests.EventFields.Overrides.Proceed.EventHandler_P_NP
{
    // <target>
    internal class Target
    {
        private event EventHandler? Foo;

        [PseudoOverride(nameof(Foo), "TestAspect1")]
        private event EventHandler Foo_Override1
        {
            add
            {
                Console.WriteLine("Override1");
            }
            remove
            {
                Console.WriteLine("Override1");
            }
        }

        [PseudoOverride(nameof(Foo), "TestAspect2")]
        private event EventHandler Foo_Override2
        {
            add
            {
                Console.WriteLine("Override2 Start");
                Link[This.Foo.add, Inline] += value;
                Console.WriteLine("Override2 End");
            }
            remove
            {
                Console.WriteLine("Override2 Start");
                Link[This.Foo.remove, Inline] -= value;
                Console.WriteLine("Override2 End");
            }
        }
    }
}
