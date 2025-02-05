using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Inlining.ReturnsVoid_NI_NI
{
    // <target>
    class Target
    {
        void Foo()
        {
            Console.WriteLine( "Original");
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        [PseudoNotInlineable]
        void Foo_Override()
        {
            Console.WriteLine( "Before");
            link( _this.Foo)();
            Console.WriteLine( "After");
        }
    }
}
