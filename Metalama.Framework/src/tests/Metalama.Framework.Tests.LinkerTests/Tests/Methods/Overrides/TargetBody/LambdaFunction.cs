// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TargetBody.LambdaFunction
{
    // <target>
    internal class Target
    {
        private int IntMethod()
        {
            if (new Random().Next() == 0)
            {
                return 0;
            }

            var foo = () =>
            {
                return;
            };

            var bar = () =>
            {
                var quz = () => 42;

                return quz();
            };

            foo();
            var x = bar();

            Console.WriteLine("Original");
            return x;
        }

        [PseudoOverride(nameof(IntMethod), "TestAspect")]
        private int IntMethod_Override()
        {
            Console.WriteLine("Before");

            var y = Link(This.IntMethod, Inline)();

            Console.WriteLine("After");

            return y;
        }


        private void VoidMethod()
        {
            if (new Random().Next() == 0)
            {
                return;
            }

            var foo = () =>
            {
                return;
            };

            var bar = () =>
            {
                var quz = () => 42;

                return quz();
            };

            foo();
            _ = bar();

            Console.WriteLine("Original");
        }

        [PseudoOverride(nameof(VoidMethod), "TestAspect")]
        private void VoidMethod_Override()
        {
            Console.WriteLine("Before");

            Link(This.VoidMethod, Inline)();

            Console.WriteLine("After");
        }
    }
}
