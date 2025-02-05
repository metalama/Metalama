using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TemplateBody.LocalFunction
{
    // <target>
    class TargetClass
    {
        int IntMethod(int x)
        {
            Console.WriteLine("Original");
            return x;
        }

        [PseudoOverride( nameof(IntMethod), "TestAspect")]
        int IntMethod_Override(int x)
        {
            return LocalFunction() + LocalFunction();

            int LocalFunction()
            {
                Console.WriteLine("Override");
                var z = link(_this.IntMethod, inline)(x);
                return z;
            }
        }

        string? StringMethod(string x)
        {
            Console.WriteLine("Original");
            return x;
        }

        [PseudoOverride(nameof(StringMethod), "TestAspect")]
        string? StringMethod_Override(string? x)
        {
            return ToUpper();

            string? ToUpper()
            {
                Console.WriteLine("Override");
                return link(_this.StringMethod, inline)(x)?.ToUpper();
            }
        }

        void VoidMethod()
        {
            Console.WriteLine("Original");
        }

        [PseudoOverride(nameof(VoidMethod), "TestAspect")]
        void VoidMethod_Override()
        {
            LocalFunction();
            LocalFunction();

            void LocalFunction()
            {
                Console.WriteLine("Override");
                link(_this.VoidMethod, inline)();
            }
        }
    }
}
