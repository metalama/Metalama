using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Overrides.Body.ExpressionBody
{
#pragma warning disable CA1805

    // <target>
    class Target
    {
        int _foo = 0;
        int Foo => this._foo;

        [PseudoOverride( nameof(Foo),"TestAspect")]
        int Foo_Override
        {
            get
            {
                Console.WriteLine( "Get");
                return link[ _this.Foo.get, inline ];
            }
        }
    }
}
