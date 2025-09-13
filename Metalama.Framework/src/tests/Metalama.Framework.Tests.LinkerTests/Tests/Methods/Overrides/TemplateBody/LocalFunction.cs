// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TemplateBody.LocalFunction
{
    // <target>
    internal class TargetClass
    {
        private int IntMethod(int x)
        {
            Console.WriteLine("Original");
            return x;
        }

        [PseudoOverride( nameof(IntMethod), "TestAspect")]
        private int IntMethod_Override(int x)
        {
            return LocalFunction() + LocalFunction();

            int LocalFunction()
            {
                Console.WriteLine("Override");
                var z = Link(This.IntMethod, Inline)(x);
                return z;
            }
        }

        private string? StringMethod(string x)
        {
            Console.WriteLine("Original");
            return x;
        }

        [PseudoOverride(nameof(StringMethod), "TestAspect")]
        private string? StringMethod_Override(string? x)
        {
            return ToUpper();

            string? ToUpper()
            {
                Console.WriteLine("Override");
                return Link(This.StringMethod, Inline)(x)?.ToUpper();
            }
        }

        private void VoidMethod()
        {
            Console.WriteLine("Original");
        }

        [PseudoOverride(nameof(VoidMethod), "TestAspect")]
        private void VoidMethod_Override()
        {
            LocalFunction();
            LocalFunction();

            static void LocalFunction()
            {
                Console.WriteLine("Override");
                Link(This.VoidMethod, Inline)();
            }
        }
    }
}
