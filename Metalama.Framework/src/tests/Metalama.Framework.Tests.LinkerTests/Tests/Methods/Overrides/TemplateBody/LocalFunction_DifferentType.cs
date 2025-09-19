// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TemplateBody.LocalFunction_DifferentType
{
    // <target>
    internal class TargetClass
    {
        private int IntMethod_VoidLocalFunction(int x)
        {
            Console.WriteLine("Original");
            return x;
        }

        [PseudoOverride( nameof(IntMethod_VoidLocalFunction), "TestAspect")]
        private int IntMethod_VoidLocalFunction_Override(int x)
        {
            LocalFunction();
            LocalFunction();

            return 42;

            void LocalFunction()
            {
                Console.WriteLine("Override");
                _ = Link(This.IntMethod_VoidLocalFunction, Inline)(x);
            }
        }

        private void VoidMethod_IntLocalFunction()
        {
            Console.WriteLine("Original");
        }

        [PseudoOverride(nameof(VoidMethod_IntLocalFunction), "TestAspect")]
        private void VoidMethod_IntLocalFunction_Override()
        {
            _ = LocalFunction();
            _ = LocalFunction();

            static int LocalFunction()
            {
                Console.WriteLine("Override");
                Link(This.VoidMethod_IntLocalFunction, Inline)();
                return 42;
            }
        }
        private int IntMethod_StringLocalFunction(int x)
        {
            Console.WriteLine("Original");
            return x;
        }

        [PseudoOverride(nameof(IntMethod_StringLocalFunction), "TestAspect")]
        private int IntMethod_StringLocalFunction_Override(int x)
        {
            return LocalFunction().Length + LocalFunction().Length;

            string LocalFunction()
            {
                Console.WriteLine("Override");
                var r = Link(This.IntMethod_StringLocalFunction, Inline)(x);
                return $"{r}";
            }
        }
    }
}
