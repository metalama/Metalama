using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Inliners.MethodAssignment_NotExactArguments
{
    // <target>
    class Target
    {
        int Foo(int y, int z)
        {
            Console.WriteLine( "Original");
            return 42;
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        int Foo_Override(int y, int z)
        {
            Console.WriteLine( "Before");
            int x;
            x = link( _this.Foo, inline)(z, y);
            Console.WriteLine( "After");
            return x;
        }
    }
}
