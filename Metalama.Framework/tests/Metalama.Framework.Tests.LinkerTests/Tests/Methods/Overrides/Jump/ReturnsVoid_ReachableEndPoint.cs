using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Jump.ReturnsVoid_ReachableEndPoint
{
    // <target>
    class Target
    {
        void Foo(int x)
        {
            Console.WriteLine( "Original Start");
            if (x == 0)
            {
                Console.WriteLine("Branch End");
                return;
            }
            else
            {
            }
            Console.WriteLine("Original End");
        }

        [PseudoOverride( nameof(Foo), "TestAspect1")]
        void Foo_Override1(int x)
        {
            Console.WriteLine( "Before1");
            if (x == 0)
            {
                link(_this.Foo, inline)(x);
                return;
            }
            Console.WriteLine( "After1");
        }

        [PseudoOverride(nameof(Foo), "TestAspect2")]
        void Foo_Override2(int x)
        {
            Console.WriteLine("Before2");
            if (x == 0)
            {
                link(_this.Foo, inline)(x);
                return;
            }
            Console.WriteLine("After2");
        }
    }
}
