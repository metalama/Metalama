using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Inliners.MethodCastReturn_NotReturnStatement
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
            return _ = (int)link( _this.Foo, inline)();
        }
    }
}
