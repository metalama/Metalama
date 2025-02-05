using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Inliners.MethodInvocation_NotExactArgumentCount
{
    // <target>
    class Target
    {
        void Foo(int x)
        {
            Console.WriteLine( "Original");
        }

        void Foo(int x, int y)
        {
            Console.WriteLine("Original");
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        void Foo_Override(int x)
        {
            Console.WriteLine( "Before");
            link( _this.Foo, inline)(x, 42);
            Console.WriteLine( "After");
        }
    }
}
