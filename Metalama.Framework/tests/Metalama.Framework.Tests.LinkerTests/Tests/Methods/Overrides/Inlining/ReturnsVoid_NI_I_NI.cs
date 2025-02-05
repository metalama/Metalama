using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Inlining.ReturnsVoid_NI_I_NI
{
    // <target>
    class Target
    {
        [PseudoNotInlineable]
        void Foo()
        {
            Console.WriteLine( "Original");
        }

        [PseudoOverride( nameof(Foo),"TestAspect1")]
        void Foo_Override1()
        {
            Console.WriteLine( "Before1");
            link( _this.Foo)();
            Console.WriteLine( "After1");
        }

        [PseudoOverride( nameof(Foo),"TestAspect2")]
        [PseudoNotInlineable]
        void Foo_Override2()
        {
            Console.WriteLine( "Before2");
            link( _this.Foo, inline)();
            Console.WriteLine( "After2");
        }
    }
}
