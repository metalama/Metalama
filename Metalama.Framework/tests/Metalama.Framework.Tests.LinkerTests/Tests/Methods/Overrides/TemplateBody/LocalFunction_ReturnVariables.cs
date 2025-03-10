// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TemplateBody.LocalFunction_ReturnVariables
{
    // <target>
    class TargetClass
    {
        int Method(int x)
        {
            Console.WriteLine("Original Begin");
            return x + 1;
        }

        [PseudoOverride( nameof(Method), "TestAspect1")]
        int Method_Override1(int x)
        {
            Console.WriteLine("Override1");
            return LocalFunction1();

            int LocalFunction1()
            {
                Console.WriteLine("Override1 Local Function");
                var y = link(_this.Method, inline)(x);
                return y;
            }
        }

        [PseudoOverride(nameof(Method), "TestAspect2")]
        int Method_Override2(int x)
        {
            Console.WriteLine("Override2");
            return LocalFunction2();

            int LocalFunction2()
            {
                Console.WriteLine("Override2 Local Function");
                var z = link(_this.Method, inline)(x);
                return z;
            }
        }
    }
}
