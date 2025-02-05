using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Overrides.Body.BlockBodies
{
    // <target>
    class Target
    {
        int Foo
        {
            get
            {
                return 42;
            }

            set
            {
            }
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        int Foo_Override
        {
            get
            {
                Console.WriteLine( "Get");
                return link[ _this.Foo.get, inline ];
            }
            set
            {
                Console.WriteLine( "Set");
                link[ _this.Foo.set, inline ] = value;
            }
        }
    }
}
