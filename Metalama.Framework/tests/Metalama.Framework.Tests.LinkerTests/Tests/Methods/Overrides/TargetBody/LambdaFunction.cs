// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TargetBody.LambdaFunction
{
    // <target>
    class Target
    {
        int IntMethod()
        {
            if (new Random().Next() == 0)
            {
                return 0;
            }

            Action foo = () =>
            {
                return;
            };

            Func<int> bar = () =>
            {
                Func<int> quz = () => 42;

                return quz();
            };

            foo();
            var x = bar();

            Console.WriteLine("Original");
            return x;
        }

        [PseudoOverride(nameof(IntMethod), "TestAspect")]
        int IntMethod_Override()
        {
            Console.WriteLine("Before");

            var y = link(_this.IntMethod, inline)();

            Console.WriteLine("After");

            return y;
        }


        void VoidMethod()
        {
            if (new Random().Next() == 0)
            {
                return;
            }

            Action foo = () =>
            {
                return;
            };

            Func<int> bar = () =>
            {
                Func<int> quz = () => 42;

                return quz();
            };

            foo();
            _ = bar();

            Console.WriteLine("Original");
        }

        [PseudoOverride(nameof(VoidMethod), "TestAspect")]
        void VoidMethod_Override()
        {
            Console.WriteLine("Before");

            link(_this.VoidMethod, inline)();

            Console.WriteLine("After");
        }
    }
}
