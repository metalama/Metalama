using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Overrides.Body.AutoProperty
{
    // <target>
    class Target
    {
        int Foo { get; set; }

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
