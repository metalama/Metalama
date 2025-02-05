using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Inliners.MethodInvocation_Void
{
    // <target>
    class Target
    {
        void Foo()
        {
            Console.WriteLine( "Original");
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        void Foo_Override()
        {
            Console.WriteLine( "Before");
            link( _this.Foo, inline)();
            Console.WriteLine( "After");
        }
    }
}
