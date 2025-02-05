using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.LinkerTests.Tests.EventFields.Overrides.Inlining.ReturnsVoid_I_NI
{
    // <target>
    class Target
    {
        event EventHandler? Foo;

        [PseudoOverride( nameof(Foo),"TestAspect")]
        event EventHandler? Foo_Override
        {
            add
            {
                Console.WriteLine("Before");
                link[_this.Foo.add] += value;
                Console.WriteLine("After");
            }

            remove
            {
                Console.WriteLine("Before");
                link[_this.Foo.remove] -= value;
                Console.WriteLine("After");
            }
        }
    }
}
