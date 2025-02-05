using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Inliners.MethodReturn_NotExactArguments
{
    // <target>
    class Target
    {
        int Foo(int x, int y)
        {
            Console.WriteLine( "Original");
            return 42;
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        int Foo_Override(int x, int y)
        {
            Console.WriteLine( "Before");
            return link( _this.Foo, inline)(y, x);
        }
    }
}
