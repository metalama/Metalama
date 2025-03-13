// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TemplateBody.LocalFunction_ComplexFlow
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
            Console.WriteLine("Override1 Begin");

            if (x > 0)
            {
                return LocalFunction1();
            }

            Console.WriteLine("Override1 End");

            return 0;

            int LocalFunction1()
            {
                Console.WriteLine("Override1 Local Function Begin");
                if (x > 0)
                {
                    return link(_this.Method, inline)(x); ;
                }

                Console.WriteLine("Override1 Local Function End");
                return 0;
            }
        }

        [PseudoOverride(nameof(Method), "TestAspect2")]
        int Method_Override2(int x)
        {
            Console.WriteLine("Override2 Begin");

            if (x > 0)
            {
                return LocalFunction1();
            }

            Console.WriteLine("Override2 End");

            return 0;

            int LocalFunction1()
            {
                Console.WriteLine("Override2 Local Function Begin");
                if (x > 0)
                {
                    return link(_this.Method, inline)(x);
                }

                Console.WriteLine("Override2 Local Function End");
                return 0;
            }
        }
    }
}
