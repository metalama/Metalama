using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Inliners.GetterAssignment_NotSameType
{
    class Base
    {
        public virtual int Foo
        {
            get
            {
                return 0;
            }
        }
    }

    // <target>
    class Target : Base
    {
        [PseudoIntroduction(nameof(Foo), "TestAspect")]
        public override int Foo
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
                int x;
                x = link( _this.Foo.get, inline, @base);
                Console.WriteLine( "After");
                return x;
            }
        }
    }
}
