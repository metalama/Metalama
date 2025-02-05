using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Jumps.ReturnsVoid_FJ
{
    // <target>
    class Target
    {
        void Foo(int x)
        {
            Console.WriteLine( "Original Start");
            if (x == 0)
            {
                return;
            }
            Console.WriteLine( "Original End");
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        void Foo_Override(int x)
        {
            Console.WriteLine( "Before");
            link( _this.Foo, inline)(x);
            Console.WriteLine( "After");
        }
    }
}
