using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Inliners.SetterAssignment_NotSimple
{
    // <target>
    class Target
    {
        int field;

        int Foo
        {
            get { return 0; }
            set
            {
                Console.WriteLine( "Original");
                this.field = value;
            }
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        int Foo_Override
        {
            get { return link[_this.Foo.set, inline]; }
            set
            {
                Console.WriteLine( "Before");
                link[ _this.Foo.set, inline ] += value;
                Console.WriteLine( "After");
            }
        }
    }
}
