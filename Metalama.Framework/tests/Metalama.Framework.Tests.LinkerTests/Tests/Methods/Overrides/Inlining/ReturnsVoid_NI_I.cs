using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Inlining.ReturnsVoid_NI_I
{
    // <target>
    class Target
    {

        void Foo()
        {
            Console.WriteLine( "Original");
        }

        [PseudoNotInlineable]
        [PseudoOverride( nameof(Foo),"TestAspect")]
        void Foo_Override()
        {
            Console.WriteLine( "Before");
            link( _this.Foo, inline)();
            Console.WriteLine( "After");
        }
    }
}
