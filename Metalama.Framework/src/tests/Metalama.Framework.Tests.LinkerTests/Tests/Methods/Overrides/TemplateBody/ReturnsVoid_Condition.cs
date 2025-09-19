// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TemplateBody.ReturnsVoid_Condition
{
    // <target>
    internal class Target
    {
        private void Foo()
        {
            Console.WriteLine( "Original");
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        private void Foo_Override()
        {
            Console.WriteLine( "Before");
            if (true)
            {
                Link( This.Foo, Inline)();
            }

            Console.WriteLine( "After");
        }
    }
}
