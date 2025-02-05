using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TargetBody.SwitchVoid_EndFlow
{
    // <target>
    class Target
    {
        void Foo(int x)
        {
            switch(x)
            {
                case 1:
                    return;
                default:
                    break;
            }
        }

        [PseudoOverride( nameof(Foo), "TestAspect")]

        void Foo_Override(int x)
        {
            Console.WriteLine("Aspect");
            link(_this.Foo, inline)(x);
        }
    }
}
