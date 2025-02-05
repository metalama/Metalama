using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.LinkerTests.Tests.EventFields.Overrides.Proceed.EventHandler_P_NP
{
    // <target>
    class Target
    {
        event EventHandler? Foo;

        [PseudoOverride(nameof(Foo), "TestAspect1")]
        event EventHandler Foo_Override1
        {
            add
            {
                Console.WriteLine("Override1");
            }
            remove
            {
                Console.WriteLine("Override1");
            }
        }

        [PseudoOverride(nameof(Foo), "TestAspect2")]
        event EventHandler Foo_Override2
        {
            add
            {
                Console.WriteLine("Override2 Start");
                link[_this.Foo.add, inline] += value;
                Console.WriteLine("Override2 End");
            }
            remove
            {
                Console.WriteLine("Override2 Start");
                link[_this.Foo.remove, inline] -= value;
                Console.WriteLine("Override2 End");
            }
        }
    }
}
