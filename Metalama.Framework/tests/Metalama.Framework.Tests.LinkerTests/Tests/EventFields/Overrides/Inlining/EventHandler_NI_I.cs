using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.LinkerTests.Tests.EventFields.Overrides.Inlining.EventHandler_NI_I
{
    // <target>
    class Target
    {
        event EventHandler? Foo;

        [PseudoNotInlineable]
        [PseudoOverride( nameof(Foo),"TestAspect")]
        event EventHandler? Foo_Override
        {
            add
            {
                Console.WriteLine("Before");
                link[_this.Foo.add, inline] += value;
                Console.WriteLine("After");
            }

            remove
            {
                Console.WriteLine("Before");
                link[_this.Foo.remove, inline] -= value;
                Console.WriteLine("After");
            }
        }
    }
}
