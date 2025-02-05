using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Jump.ReturnsInt_Expression
{
    // <target>
    class Target
    {
        int Foo(int x)
        {
            Console.WriteLine( "Original Start");
            if (x == 0)
            {
                return 42;
            }
            Console.WriteLine( "Original End");
            return x;
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        int Foo_Override(int x) => link( _this.Foo, inline)(x);
    }
}
