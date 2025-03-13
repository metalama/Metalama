// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TemplateBody.LocalFunction_DifferentType
{
    // <target>
    class TargetClass
    {
        int IntMethod_VoidLocalFunction(int x)
        {
            Console.WriteLine("Original");
            return x;
        }

        [PseudoOverride( nameof(IntMethod_VoidLocalFunction), "TestAspect")]
        int IntMethod_VoidLocalFunction_Override(int x)
        {
            LocalFunction();
            LocalFunction();

            return 42;

            void LocalFunction()
            {
                Console.WriteLine("Override");
                _ = link(_this.IntMethod_VoidLocalFunction, inline)(x);
            }
        }

        void VoidMethod_IntLocalFunction()
        {
            Console.WriteLine("Original");
        }

        [PseudoOverride(nameof(VoidMethod_IntLocalFunction), "TestAspect")]
        void VoidMethod_IntLocalFunction_Override()
        {
            _ = LocalFunction();
            _ = LocalFunction();

            int LocalFunction()
            {
                Console.WriteLine("Override");
                link(_this.VoidMethod_IntLocalFunction, inline)();
                return 42;
            }
        }
        int IntMethod_StringLocalFunction(int x)
        {
            Console.WriteLine("Original");
            return x;
        }

        [PseudoOverride(nameof(IntMethod_StringLocalFunction), "TestAspect")]
        int IntMethod_StringLocalFunction_Override(int x)
        {
            return LocalFunction().Length + LocalFunction().Length;

            string LocalFunction()
            {
                Console.WriteLine("Override");
                var r = link(_this.IntMethod_StringLocalFunction, inline)(x);
                return $"{r}";
            }
        }
    }
}
