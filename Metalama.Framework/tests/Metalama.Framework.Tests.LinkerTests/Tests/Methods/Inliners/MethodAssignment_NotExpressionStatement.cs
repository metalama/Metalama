using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Inliners.MethodAssignment_NotExpressionStatement
{
    // <target>
    class Target
    {
        int Foo()
        {
            Console.WriteLine( "Original");
            return 42;
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        int Foo_Override()
        {
            Console.WriteLine( "Before");
            int x = 0;
            x = x = link( _this.Foo, inline)();
            Console.WriteLine( "After");
            return x;
        }
    }
}
