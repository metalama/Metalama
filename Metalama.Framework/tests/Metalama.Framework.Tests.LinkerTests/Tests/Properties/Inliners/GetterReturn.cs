using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Inliners.GetterReturn
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
                return link( _this.Foo.get, inline);
            }
        }
    }
}
