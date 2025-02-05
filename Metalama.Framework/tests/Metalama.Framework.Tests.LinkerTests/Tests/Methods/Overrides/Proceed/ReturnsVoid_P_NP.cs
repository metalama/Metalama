using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Proceed.ReturnsVoid_P_NP
{
    // <target>
    class Target
    {
        void Foo(int x)
        {
            Console.WriteLine( "Original");
        }

        [PseudoOverride( nameof(Foo),"TestAspect1")]
        void Foo_Override1(int x)
        {
            Console.WriteLine( "Override1");
        }

        [PseudoOverride( nameof(Foo),"TestAspect2")]
        void Foo_Override2(int x)
        {
            Console.WriteLine( "Override2 Start");
            link( _this.Foo, inline)(x);
            Console.WriteLine( "Override2 End");
        }
    }
}
