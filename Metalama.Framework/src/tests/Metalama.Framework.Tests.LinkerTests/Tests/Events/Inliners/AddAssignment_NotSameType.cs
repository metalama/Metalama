// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Events.Inliners.AddAssignment_NotSameType
{
    public class Base
    {
        protected virtual event EventHandler Foo
        {
            add { }
            remove { }
        }
    }


    // <target>
    public class Target : Base
    {
        [PseudoIntroduction("TestAspect")]
        protected override event EventHandler Foo
        {
            add { }
            remove { }
        }

        [PseudoOverride(nameof(Foo), "TestAspect")]
        private event EventHandler Foo_Override
        {
            add
            {
                Console.WriteLine("Before");
                link[_this.Foo.add, inline, @base] += value;
                Console.WriteLine("After");
            }
            remove { }
        }
    }
}
