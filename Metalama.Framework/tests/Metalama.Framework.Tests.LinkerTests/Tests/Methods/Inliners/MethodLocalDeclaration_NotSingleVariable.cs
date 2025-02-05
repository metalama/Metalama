using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0219

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Inliners.MethodLocalDeclaration_NotSingleVariable
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
            int y = 0, x = link( _this.Foo, inline)();
            Console.WriteLine( "After");
            return x;
        }
    }
}
