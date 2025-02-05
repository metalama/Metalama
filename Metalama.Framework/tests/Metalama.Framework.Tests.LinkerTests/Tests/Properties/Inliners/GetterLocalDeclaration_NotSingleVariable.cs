using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0219

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Inliners.GetterLocalDeclaration_NotSingleVariable
{
    // <target>
    class Target
    {
        int Foo
        {
            get
            {
                Console.WriteLine( "Original");
                return 42;
            }
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        int Foo_Override
        {
            get
            {
                Console.WriteLine( "Before");
                int y = 0, x = link( _this.Foo.get, inline);
                Console.WriteLine( "After");
                return x;
            }
        }
    }
}
