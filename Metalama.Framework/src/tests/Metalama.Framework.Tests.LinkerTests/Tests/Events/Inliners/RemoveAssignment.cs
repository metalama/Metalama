// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Events.Inliners.RemoveAssignment
{
    // <target>
    public class Target
    {
        private EventHandler? _field;

        public event EventHandler? Foo
        {
            add { }
            remove
            {
                Console.WriteLine("Original");
                this._field -= value;
            }
        }

        [PseudoOverride(nameof(Foo), "TestAspect")]
        public event EventHandler? Foo_Override
        {
            add { }
            remove
            {
                Console.WriteLine("Before");
                Link[This.Foo.remove, Inline] -= value;
                Console.WriteLine("After");
            }
        }
    }
}
